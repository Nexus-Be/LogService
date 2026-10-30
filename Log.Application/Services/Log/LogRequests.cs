namespace Log.Application.Services.Log;

using System;

public sealed record QueryLogsRequest(
    int Page = 1,
    int PageSize = 20,
    string? From = null,
    string? To = null,
    string? ServiceName = null,
    string? EventType = null,
    string? EntityName = null,
    string? EntityId = null,
    string? ActorUserId = null,
    string? CorrelationId = null,
    string? LogLevel = null);

public sealed record SetLogEventTypeEnabledRequest(
    string Code,
    bool IsEnabled);
