namespace Log.Application.Services.Log;

using System;
using System.Collections.Generic;

public sealed record LogEntryResponse(
    Guid Id,
    DateTimeOffset OccurredAt,
    string ServiceName,
    string EventType,
    string Action,
    string EntityName,
    string EntityId,
    string ActorUserId,
    string ActorRole,
    string CorrelationId,
    string TraceId,
    string LogLevel,
    string Payload);

public sealed record LogEventTypeResponse(
    string Code,
    string ServiceName,
    string Category,
    string Description,
    bool IsEnabled,
    DateTimeOffset UpdatedAt);

public sealed record LogEventTypeListResponse(
    IReadOnlyList<LogEventTypeResponse> Items,
    int Total);
