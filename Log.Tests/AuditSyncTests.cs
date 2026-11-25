namespace Log.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using global::Log.Application.Interfaces;
using global::Log.Application.Services.AuditSync;
using global::Log.Domain.Entities;
using global::Log.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class TestLogDb : DbContext, IApplicationDbContext
{
    public TestLogDb(DbContextOptions<TestLogDb> options) : base(options) { }
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<LogEventType> LogEventTypes => Set<LogEventType>();
    public DbSet<CentralAuditLog> CentralAuditLogs => Set<CentralAuditLog>();
    public DbSet<AuditSyncBatch> AuditSyncBatches => Set<AuditSyncBatch>();
    public DbSet<SecurityAuditAlert> SecurityAuditAlerts => Set<SecurityAuditAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<LogEntry>();
        modelBuilder.Ignore<LogEventType>();
    }
}

public sealed class FakeSources : IAuditSourceClient
{
    public Dictionary<string, List<SourceAuditItem>> Pending { get; } = new();
    public HashSet<string> Down { get; } = new();
    public List<(string Source, Guid Id)> Marked { get; } = new();
    public IReadOnlyList<string> SourceNames => new[] { "wallet-service", "user-service" };

    public Task<SourceAuditPage> GetUnsyncedAsync(string source, int limit, CancellationToken ct)
    {
        if (Down.Contains(source)) throw new HttpRequestException("down");
        var items = Pending.TryGetValue(source, out var list) ? list.Where(i => !Marked.Any(m => m.Id == i.Id)).ToList() : new List<SourceAuditItem>();
        return Task.FromResult(new SourceAuditPage(items.Take(limit).ToList(), items.Count));
    }

    public Task MarkSyncedAsync(string source, IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        foreach (var id in ids) Marked.Add((source, id));
        return Task.CompletedTask;
    }
}

public class AuditSyncTests
{
    private static TestLogDb NewDb() => new(new DbContextOptionsBuilder<TestLogDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static SourceAuditItem Item(string service, string action, string? newValues = null, string? user = "u1", DateTimeOffset? at = null, string entity = "WalletAccount") =>
        new(Guid.NewGuid(), service, "trace", user, null, action, entity, "e1", null, newValues, null, null, null, at ?? DateTimeOffset.UtcNow);

    private static (AuditSyncRunner Runner, AuditSyncStateStore State, TestLogDb Db, FakeSources Sources) Build(AuditSyncOptions? options = null)
    {
        var db = NewDb();
        var sources = new FakeSources();
        var state = new AuditSyncStateStore();
        var opts = Options.Create(options ?? new AuditSyncOptions());
        var rules = new SecurityRuleEvaluator(db, opts);
        var runner = new AuditSyncRunner(db, sources, new AuditRedactor(), rules, state, opts, NullLogger<AuditSyncRunner>.Instance);
        return (runner, state, db, sources);
    }

    [Theory]
    [InlineData("BankAccountNumber")]
    [InlineData("AccountNumber")]
    [InlineData("BankName")]
    [InlineData("BankAccountHolder")]
    [InlineData("VerificationEvidence")]
    [InlineData("PasswordHash")]
    [InlineData("Token")]
    [InlineData("Secret")]
    [InlineData("RefreshToken")]
    [InlineData("ApiKey")]
    public void Redactor_hides_every_listed_property(string name)
    {
        var redacted = new AuditRedactor().Redact($"{{\"{name}\":\"123456789\",\"Status\":\"Open\"}}")!;
        redacted.Should().NotContain("123456789");
        redacted.Should().Contain(AuditRedactor.Marker);
        redacted.Should().Contain("Open");
    }

    [Fact]
    public void Redactor_reaches_into_nested_objects_and_arrays_and_keeps_plain_notes_valid()
    {
        var redactor = new AuditRedactor();
        redactor.Redact("{\"a\":{\"bank\":{\"accountNumber\":\"999\"}},\"list\":[{\"token\":\"t\"}]}").Should().NotContain("999").And.NotContain("\"t\"");
        redactor.Redact("just a note").Should().Be("\"just a note\"");
        redactor.Redact("  ").Should().BeNull();
        redactor.Redact(null).Should().BeNull();
    }

    [Fact]
    public async Task Sync_stores_each_record_once_marks_it_and_redacts()
    {
        var (runner, _, db, sources) = Build();
        var item = Item("wallet-service", "WithdrawalRequest", "{\"BankAccountNumber\":\"111222333\",\"Amount\":50}");
        sources.Pending["wallet-service"] = new() { item };

        (await runner.RunOnceAsync(default)).Should().Be(1);
        // The source offers it again (it was not marked yet in this fake): nothing is stored twice.
        sources.Marked.Clear();
        (await runner.RunOnceAsync(default)).Should().Be(0);

        var stored = await db.CentralAuditLogs.SingleAsync();
        stored.SourceRecordId.Should().Be(item.Id);
        stored.NewValues.Should().NotContain("111222333");
        stored.ActionName.Should().Be("WithdrawalRequest");
        sources.Marked.Should().Contain((("wallet-service"), item.Id));
        (await db.AuditSyncBatches.CountAsync()).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task A_service_that_is_down_is_Unknown_until_it_was_read_then_Unavailable_and_loses_nothing()
    {
        var (runner, state, db, sources) = Build();
        sources.Down.Add("user-service");
        await runner.RunOnceAsync(default);
        state.Get("user-service").State.Should().Be(AuditSyncStates.Unknown);
        state.Get("user-service").UnsyncedCount.Should().BeNull();

        sources.Down.Clear();
        sources.Pending["user-service"] = new() { Item("user-service", "UserUpdate", entity: "Users") };
        await runner.RunOnceAsync(default);
        state.Get("user-service").State.Should().Be(AuditSyncStates.Known);

        sources.Down.Add("user-service");
        await runner.RunOnceAsync(default);
        var status = state.Get("user-service");
        status.State.Should().Be(AuditSyncStates.Unavailable);
        status.UnsyncedCount.Should().BeNull();
        status.LastKnownUnsyncedCount.Should().NotBeNull();
        (await db.CentralAuditLogs.CountAsync()).Should().Be(1);
        (await db.AuditSyncBatches.AnyAsync(b => b.Status == AuditSyncStatus.Failed)).Should().BeTrue();
    }

    [Fact]
    public async Task Large_balance_adjustment_raises_BalanceTampering_and_a_small_one_does_not()
    {
        var (runner, _, db, sources) = Build();
        sources.Pending["wallet-service"] = new()
        {
            Item("wallet-service", "BalanceAdjustment", "{\"Balance\":100,\"Amount\":50000,\"Type\":\"AdjustmentCredit\"}", user: "admin1"),
            Item("wallet-service", "BalanceAdjustment", "{\"Balance\":100,\"Amount\":10000000,\"Type\":\"AdjustmentCredit\"}", user: "admin2"),
        };
        await runner.RunOnceAsync(default);
        var alerts = await db.SecurityAuditAlerts.ToListAsync();
        alerts.Should().ContainSingle().Which.TriggeredRule.Should().Be(SecurityRuleType.BalanceTampering);
    }

    [Fact]
    public async Task Repeated_small_adjustments_by_one_admin_raise_BalanceTampering()
    {
        var (runner, _, db, sources) = Build(new AuditSyncOptions { BalanceAdjustmentCount = 3 });
        var now = DateTimeOffset.UtcNow;
        sources.Pending["wallet-service"] = Enumerable.Range(0, 3)
            .Select(i => Item("wallet-service", "BalanceAdjustment", "{\"Amount\":1000}", user: "admin1", at: now.AddSeconds(i))).ToList();
        await runner.RunOnceAsync(default);
        (await db.SecurityAuditAlerts.CountAsync(a => a.TriggeredRule == SecurityRuleType.BalanceTampering)).Should().Be(1);
    }

    [Fact]
    public async Task Role_change_raises_PrivilegeEscalation_and_an_ordinary_update_raises_nothing()
    {
        var (runner, _, db, sources) = Build();
        sources.Pending["user-service"] = new()
        {
            Item("user-service", "RoleAssign", entity: "Users"),
            Item("user-service", "UserUpdate", entity: "Users"),
            Item("user-service", "ReputationAdjust", entity: "ReputationScore"),
        };
        await runner.RunOnceAsync(default);
        (await db.SecurityAuditAlerts.ToListAsync()).Should().ContainSingle().Which.TriggeredRule.Should().Be(SecurityRuleType.PrivilegeEscalation);
    }

    [Fact]
    public async Task Ten_deletes_by_one_user_in_the_window_raise_one_MassDelete_alert_and_nine_do_not()
    {
        var (runner, _, db, sources) = Build();
        var now = DateTimeOffset.UtcNow;
        sources.Pending["user-service"] = Enumerable.Range(0, 9).Select(i => Item("user-service", "SoftDelete", user: "admin1", at: now.AddSeconds(i), entity: "Product")).ToList();
        await runner.RunOnceAsync(default);
        (await db.SecurityAuditAlerts.CountAsync()).Should().Be(0);

        sources.Marked.Clear();
        sources.Pending["user-service"] = Enumerable.Range(9, 3).Select(i => Item("user-service", "SoftDelete", user: "admin1", at: now.AddSeconds(i), entity: "Product")).ToList();
        await runner.RunOnceAsync(default);
        (await db.SecurityAuditAlerts.CountAsync(a => a.TriggeredRule == SecurityRuleType.MassDelete)).Should().Be(1);
    }

    [Fact]
    public async Task Closing_an_alert_needs_a_note_and_records_who_closed_it()
    {
        var (runner, state, db, sources) = Build();
        sources.Pending["user-service"] = new() { Item("user-service", "RoleAssign", entity: "Users") };
        await runner.RunOnceAsync(default);
        var alert = await db.SecurityAuditAlerts.SingleAsync();

        var service = new AdminAuditService(db, sources, new NoHealth(), state, Options.Create(new AuditSyncOptions()));
        var noNote = async () => await service.UpdateAlertAsync(alert.Id, new UpdateSecurityAlertRequest("Resolved", "  "), Guid.NewGuid(), default);
        await noNote.Should().ThrowAsync<global::Log.Domain.Exceptions.DomainException>();

        var admin = Guid.NewGuid();
        var closed = await service.UpdateAlertAsync(alert.Id, new UpdateSecurityAlertRequest("Resolved", "Checked with the admin"), admin, default);
        closed.ResolvedBy.Should().Be(admin);
        closed.Status.Should().Be(AlertStatus.Resolved);

        var again = async () => await service.UpdateAlertAsync(alert.Id, new UpdateSecurityAlertRequest("Resolved", "again"), admin, default);
        await again.Should().ThrowAsync<global::Log.Domain.Exceptions.DomainException>();
    }

    private sealed class NoHealth : IHealthProbe
    {
        public IReadOnlyList<string> ServiceNames => Array.Empty<string>();
        public Task<ServiceHealthResponse> CheckAsync(string service, CancellationToken ct) => throw new NotSupportedException();
    }
}
