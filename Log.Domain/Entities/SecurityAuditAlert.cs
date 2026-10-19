using Log.Domain.Base;
using Log.Domain.Enums;

namespace Log.Domain.Entities;

public class SecurityAuditAlert : BaseEntity<Guid>
{
    public required string AlertTitle { get; set; }
    public required AlertSeverity Severity { get; set; } = AlertSeverity.High;
    public required string ServiceName { get; set; }
    public required Guid CentralAuditLogId { get; set; }
    public required SecurityRuleType TriggeredRule { get; set; }
    public string? Description { get; set; }
    public required AlertStatus Status { get; set; } = AlertStatus.Open;
    public Guid? ResolvedBy { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }

    // Navigation
    public CentralAuditLog? CentralAuditLog { get; set; }
}
