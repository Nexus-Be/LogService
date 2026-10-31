namespace Log.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Log.Api.Filters;
using Log.Application.Services.Log;
using global::Log.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/v1/logs")]
[RequireRole("admin", "staff")]
public class LogController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogController> _logger;

    public LogController(ILogService logService, ILogger<LogController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>Queries and filters centralized logs.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedData<LogEntryResponse>>> QueryLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? from = null,
        [FromQuery] string? to = null,
        [FromQuery] string? serviceName = null,
        [FromQuery] string? eventType = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? actorUserId = null,
        [FromQuery] string? correlationId = null,
        [FromQuery] string? logLevel = null,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        _logger.LogInformation("Querying logs: page {Page}, size {PageSize}, service {ServiceName}", page, pageSize, serviceName);

        var request = new QueryLogsRequest(
            page,
            pageSize,
            from,
            to,
            serviceName,
            eventType,
            entityName,
            entityId,
            actorUserId,
            correlationId,
            logLevel
        );

        var result = await _logService.QueryLogsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a single log entry by ID.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LogEntryResponse>> GetLog([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching log by ID: {Id}", id);
        var item = await _logService.GetLogByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    /// <summary>Lists all registered log event types.</summary>
    [HttpGet("event-types")]
    public async Task<ActionResult<LogEventTypeListResponse>> ListLogEventTypes(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing all log event types");
        var result = await _logService.ListEventTypesAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>Enables or disables a specific log event type.</summary>
    [HttpPut("event-types/{code}")]
    public async Task<ActionResult<LogEventTypeResponse>> SetLogEventTypeEnabled(
        [FromRoute] string code,
        [FromBody] SetLogEventTypeEnabledRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedRequest = request with { Code = code.Trim() };
        _logger.LogInformation("Setting event type {Code} enabled: {Enabled}", code, request?.IsEnabled);
        var result = await _logService.SetEventTypeEnabledAsync(normalizedRequest, cancellationToken);
        return Ok(result);
    }
}
