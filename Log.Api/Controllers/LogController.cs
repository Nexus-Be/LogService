namespace Log.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Log.Application.DTOs;
using Log.Application.Features.Commands.SetLogEventTypeEnabled;
using Log.Application.Features.Queries.GetLog;
using Log.Application.Features.Queries.ListLogEventTypes;
using Log.Application.Features.Queries.QueryLogs;

[ApiController]
[Route("api/v1")]
public sealed class LogController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogController> _logger;

    public LogController(IMediator mediator, ILogger<LogController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("log/logs")]
    [HttpGet("admin/logs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
        _logger.LogInformation("[REST QueryLogs] Query logs page: {Page}, size: {PageSize}, service: {ServiceName}", page, pageSize, serviceName);

        var query = new QueryLogsQuery(
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

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(new
        {
            items = result.Items,
            totalItems = result.Total,
            total = result.Total,
            pageNumber = page,
            pageSize = pageSize,
            totalPages = (int)Math.Ceiling((double)result.Total / (pageSize <= 0 ? 20 : pageSize))
        });
    }

    [HttpGet("log/logs/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLog(string id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var guidId))
        {
            return BadRequest(new { title = "Invalid input", detail = "Log ID must be a valid GUID.", errorCode = "VAL_INVALID_INPUT" });
        }

        _logger.LogInformation("[REST GetLog] Fetching log by Id: '{Id}'", id);
        var item = await _mediator.Send(new GetLogQuery(guidId), cancellationToken);
        if (item == null)
        {
            return NotFound(new { title = "Not found", detail = "Log entry not found.", errorCode = "LOG_NOT_FOUND" });
        }

        return Ok(item);
    }

    [HttpGet("log/event-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListLogEventTypes(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[REST ListLogEventTypes] Fetching all log event types");
        var result = await _mediator.Send(new ListLogEventTypesQuery(), cancellationToken);

        return Ok(new
        {
            items = result.Items,
            total = result.Total
        });
    }

    [HttpPut("log/event-types/{code}")]
    [HttpPut("log/event-types/{code}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetLogEventTypeEnabled(string code, [FromBody] SetLogEventTypeEnabledApiRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return BadRequest(new { title = "Invalid input", detail = "Event type code is required.", errorCode = "VAL_INVALID_INPUT" });
        }

        _logger.LogInformation("[REST SetLogEventTypeEnabled] Code: '{Code}', Enabled: {Enabled}", code, request.IsEnabled);
        var result = await _mediator.Send(new SetLogEventTypeEnabledCommand(code.Trim(), request.IsEnabled), cancellationToken);

        return Ok(result);
    }

    [HttpPost("log/batch")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult BatchLog([FromBody] object payload)
    {
        _logger.LogInformation("[REST BatchLog] Received log batch ingestion request");
        return Accepted(new { success = true, message = "Log batch accepted for processing." });
    }
}

public sealed record SetLogEventTypeEnabledApiRequest(
    bool IsEnabled
);
