using Log.Domain.Base;
using Log.Domain.Enums;

namespace Log.Domain.Entities;

public class AuditSyncBatch : BaseEntity<Guid>
{
    public required string ServiceName { get; set; }
    public required int BatchSize { get; set; }
    public string? FirstRecordId { get; set; }
    public string? LastRecordId { get; set; }
    public required AuditSyncStatus Status { get; set; } = AuditSyncStatus.Completed;
    public required DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
