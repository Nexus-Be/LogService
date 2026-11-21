using Log.Domain.Base;
using Log.Domain.Enums;

namespace Log.Domain.Entities;

public class CentralAuditLog : BaseEntity<Guid>
{
    public required string ServiceName { get; set; }
    public string? TraceId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public required CentralAuditAction Action { get; set; }
    public required string EntityName { get; set; }
    public required string EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedColumns { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    /// <summary>The id of the record in the service it came from; with ServiceName it makes the sync idempotent.</summary>
    public Guid? SourceRecordId { get; set; }
    /// <summary>The action name as the source service spells it (for example BalanceAdjustment).</summary>
    public string? ActionName { get; set; }
    public DateTimeOffset SyncedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<SecurityAuditAlert> SecurityAlerts { get; set; } = new List<SecurityAuditAlert>();
}
