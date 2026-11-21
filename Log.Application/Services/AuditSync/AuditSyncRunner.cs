namespace Log.Application.Services.AuditSync;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using global::Log.Application.Interfaces;
using global::Log.Domain.Entities;
using global::Log.Domain.Enums;

/// <summary>What the service last learned about each source. Held in memory: after a restart every source is Unknown until it is read again.</summary>
public sealed class AuditSyncStateStore
{
    private readonly ConcurrentDictionary<string, SourceSyncStatus> _states = new();
    public DateTimeOffset? LastBatchAt { get; private set; }

    public void RecordSuccess(string source, int unsynced, bool synced)
    {
        var now = DateTimeOffset.UtcNow;
        var previous = Get(source);
        _states[source] = new SourceSyncStatus(source, AuditSyncStates.Known, unsynced, unsynced, now,
            synced ? now : previous.LastSuccessfulSyncAt, null);
        if (synced) LastBatchAt = now;
    }

    public void RecordFailure(string source, string error)
    {
        var previous = Get(source);
        var hadData = previous.State != AuditSyncStates.Unknown;
        _states[source] = new SourceSyncStatus(source,
            hadData ? AuditSyncStates.Unavailable : AuditSyncStates.Unknown,
            null,
            previous.LastKnownUnsyncedCount ?? previous.UnsyncedCount,
            previous.ObservedAt,
            previous.LastSuccessfulSyncAt,
            error.Length > 300 ? error[..300] : error);
    }

    public SourceSyncStatus Get(string source) =>
        _states.TryGetValue(source, out var state) ? state : new SourceSyncStatus(source, AuditSyncStates.Unknown, null, null, null, null, null);
}

/// <summary>Pulls audit records from the source services and stores them centrally (A3). Safe to run again: a record already stored is skipped.</summary>
public sealed class AuditSyncRunner
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditSourceClient _sources;
    private readonly IAuditRedactor _redactor;
    private readonly SecurityRuleEvaluator _rules;
    private readonly AuditSyncStateStore _state;
    private readonly AuditSyncOptions _options;
    private readonly ILogger<AuditSyncRunner> _logger;

    public AuditSyncRunner(
        IApplicationDbContext db,
        IAuditSourceClient sources,
        IAuditRedactor redactor,
        SecurityRuleEvaluator rules,
        AuditSyncStateStore state,
        IOptions<AuditSyncOptions> options,
        ILogger<AuditSyncRunner> logger)
    {
        _db = db;
        _sources = sources;
        _redactor = redactor;
        _rules = rules;
        _state = state;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Runs one pass over every source. Returns the number of records stored.</summary>
    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var stored = 0;
        foreach (var source in _sources.SourceNames)
        {
            ct.ThrowIfCancellationRequested();
            stored += await SyncSourceAsync(source, ct);
        }

        return stored;
    }

    private async Task<int> SyncSourceAsync(string source, CancellationToken ct)
    {
        var batch = new AuditSyncBatch
        {
            Id = Guid.NewGuid(),
            ServiceName = source,
            BatchSize = 0,
            Status = AuditSyncStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow
        };

        SourceAuditPage page;
        try
        {
            page = await _sources.GetUnsyncedAsync(source, Math.Clamp(_options.BatchSize, 1, 500), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _state.RecordFailure(source, ex.Message);
            _logger.LogWarning("Audit source {Source} could not be read: {Message}", source, ex.Message);
            batch.Status = AuditSyncStatus.Failed;
            batch.ErrorMessage = Truncate(ex.Message);
            batch.CompletedAt = DateTimeOffset.UtcNow;
            _db.AuditSyncBatches.Add(batch);
            await _db.SaveChangesAsync(ct);
            return 0;
        }

        if (page.Items.Count == 0)
        {
            _state.RecordSuccess(source, page.UnsyncedCount, synced: false);
            return 0;
        }

        var ids = page.Items.Select(i => i.Id).ToList();
        var known = await _db.CentralAuditLogs
            .Where(c => c.ServiceName == source && c.SourceRecordId != null && ids.Contains(c.SourceRecordId.Value))
            .Select(c => c.SourceRecordId!.Value)
            .ToListAsync(ct);
        var knownSet = known.ToHashSet();

        var fresh = new List<CentralAuditLog>();
        foreach (var item in page.Items.Where(i => !knownSet.Contains(i.Id)))
        {
            fresh.Add(new CentralAuditLog
            {
                Id = Guid.NewGuid(),
                SourceRecordId = item.Id,
                ServiceName = source,
                TraceId = item.TraceId,
                UserId = item.UserId,
                UserName = item.UserName,
                Action = MapAction(item.Action),
                ActionName = item.Action,
                EntityName = item.EntityName,
                EntityId = item.EntityId,
                OldValues = _redactor.Redact(item.OldValues),
                NewValues = _redactor.Redact(item.NewValues),
                ChangedColumns = _redactor.Redact(item.ChangedColumns),
                IpAddress = item.IpAddress,
                UserAgent = item.UserAgent,
                CreatedAt = item.CreatedAt,
                UpdatedAt = DateTimeOffset.UtcNow,
                SyncedAt = DateTimeOffset.UtcNow
            });
        }

        try
        {
            _db.CentralAuditLogs.AddRange(fresh);
            batch.BatchSize = page.Items.Count;
            batch.FirstRecordId = page.Items[0].Id.ToString();
            batch.LastRecordId = page.Items[^1].Id.ToString();
            batch.CompletedAt = DateTimeOffset.UtcNow;
            _db.AuditSyncBatches.Add(batch);
            await _db.SaveChangesAsync(ct);

            if (fresh.Count > 0)
            {
                await _rules.EvaluateAsync(fresh, ct);
            }

            // Only after the records are stored here is the source told it can stop offering them.
            await _sources.MarkSyncedAsync(source, ids, ct);
            _state.RecordSuccess(source, Math.Max(0, page.UnsyncedCount - ids.Count), synced: true);
            return fresh.Count;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Stored but not acknowledged is fine: the next pass sees them as known and acknowledges them.
            _state.RecordFailure(source, ex.Message);
            _logger.LogWarning("Audit sync for {Source} did not finish: {Message}", source, ex.Message);
            return 0;
        }
    }

    private static CentralAuditAction MapAction(string name) => name switch
    {
        "Create" => CentralAuditAction.Create,
        "Update" => CentralAuditAction.Update,
        "Delete" => CentralAuditAction.Delete,
        "SoftDelete" => CentralAuditAction.SoftDelete,
        "Login" => CentralAuditAction.Login,
        "Logout" => CentralAuditAction.Logout,
        "Approve" => CentralAuditAction.Approve,
        "Reject" => CentralAuditAction.Reject,
        "Export" => CentralAuditAction.Export,
        _ => CentralAuditAction.Other
    };

    private static string Truncate(string value) => value.Length > 1000 ? value[..1000] : value;
}
