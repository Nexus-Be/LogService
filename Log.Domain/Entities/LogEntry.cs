namespace Log.Domain.Entities;

using global::Log.Domain.Common;

public class LogEntry : BaseEntity
{
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset RecordedAt { get; set; } = DateTimeOffset.UtcNow;
    public string ServiceName { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ActorUserId { get; set; } = string.Empty;
    public string ActorUsername { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string CausationId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public int SchemaVersion { get; set; } = 1;
    public string TraceId { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public string LogLevel { get; set; } = "INFO";
}
