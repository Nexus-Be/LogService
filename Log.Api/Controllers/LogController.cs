namespace Log.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Log.Application.Services.Log;

[ApiController]
[Route("api/v1/log")]
public sealed class LogController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogController> _logger;

    public LogController(ILogService logService, ILogger<LogController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    [HttpGet("logs")]
    [ProducesResponseType(typeof(LogPageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> QueryLogs(
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

    [HttpGet("logs/{id}")]
    [ProducesResponseType(typeof(LogEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLog(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BadRequest(new { title = "Invalid input", detail = "Log ID must be a valid GUID.", errorCode = "VAL_INVALID_INPUT" });
        }

        _logger.LogInformation("Fetching log by ID: {Id}", id);
        var item = await _logService.GetLogByIdAsync(guidId, cancellationToken);
        if (item == null)
        {
            return NotFound(new { title = "Not found", detail = "Log entry not found.", errorCode = "LOG_NOT_FOUND" });
        }

        return Ok(item);
    }

    [HttpGet("event-types")]
    [ProducesResponseType(typeof(LogEventTypeListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLogEventTypes(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Listing all log event types");
        var result = await _logService.ListEventTypesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut("event-types/{code}")]
    [ProducesResponseType(typeof(LogEventTypeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLogEventTypeEnabled(
        string code,
        [FromBody] SetLogEventTypeEnabledRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { title = "Invalid input", detail = "Event type code is required.", errorCode = "VAL_INVALID_INPUT" });
        }

        var normalizedRequest = request with { Code = code.Trim() };
        _logger.LogInformation("Setting event type {Code} enabled: {Enabled}", code, request.IsEnabled);
        var result = await _logService.SetEventTypeEnabledAsync(normalizedRequest, cancellationToken);
        return Ok(result);
    }

    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> BatchLog([FromBody] object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received log batch ingestion request");
        await _logService.IngestBatchAsync(payload, cancellationToken);
        return Accepted(new { success = true, message = "Log batch accepted for processing." });
    }
}
