namespace Log.Application.DTOs;

public class LogEntryDto
{
    public string Id { get; set; } = string.Empty;
    public string OccurredAt { get; set; } = string.Empty;
    public string RecordedAt { get; set; } = string.Empty;
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
    public string Payload { get; set; } = string.Empty;
    public string LogLevel { get; set; } = "INFO";
}

public class LogPageDto
{
    public IReadOnlyList<LogEntryDto> Items { get; set; } = Array.Empty<LogEntryDto>();
    public int Total { get; set; }
}

public class LogEventTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public class LogEventTypeListDto
{
    public IReadOnlyList<LogEventTypeDto> Items { get; set; } = Array.Empty<LogEventTypeDto>();
    public int Total { get; set; }
}
