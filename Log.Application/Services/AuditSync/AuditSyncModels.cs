namespace Log.Application.Services.AuditSync;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>An audit record as a source service hands it over.</summary>
public sealed record SourceAuditItem(
    Guid Id,
    string ServiceName,
    string? TraceId,
    string? UserId,
    string? UserName,
    string Action,
    string EntityName,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? ChangedColumns,
    string? IpAddress,
    string? UserAgent,
    DateTimeOffset CreatedAt);

public sealed record SourceAuditPage(IReadOnlyList<SourceAuditItem> Items, int UnsyncedCount);

/// <summary>Reads audit records from one source service. Throws when the service cannot be reached.</summary>
public interface IAuditSourceClient
{
    IReadOnlyList<string> SourceNames { get; }

    Task<SourceAuditPage> GetUnsyncedAsync(string source, int limit, CancellationToken ct);

    Task MarkSyncedAsync(string source, IReadOnlyList<Guid> ids, CancellationToken ct);
}

public static class AuditSyncStates
{
    public const string Known = "Known";
    public const string Unavailable = "Unavailable";
    public const string Unknown = "Unknown";
}

public sealed record SourceSyncStatus(
    string ServiceName,
    string State,
    int? UnsyncedCount,
    int? LastKnownUnsyncedCount,
    DateTimeOffset? ObservedAt,
    DateTimeOffset? LastSuccessfulSyncAt,
    string? LastError);

public sealed record AuditSyncStatusResponse(
    bool Enabled,
    DateTimeOffset? LastBatchAt,
    IReadOnlyList<SourceSyncStatus> Services);

public sealed class AuditSyncOptions
{
    public bool Enabled { get; set; }
    public int IntervalSeconds { get; set; } = 15;
    public int BatchSize { get; set; } = 200;
    public int RetentionDays { get; set; } = 365;
    public int BalanceAdjustmentThreshold { get; set; } = 5_000_000;
    public int BalanceAdjustmentCount { get; set; } = 3;
    public int BalanceAdjustmentWindowMinutes { get; set; } = 10;
    public int MassDeleteCount { get; set; } = 10;
    public int MassDeleteWindowMinutes { get; set; } = 5;
}
