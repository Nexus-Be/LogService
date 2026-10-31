namespace Log.Api.Controllers.Public;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Log.Application.Services.Log;

[ApiController]
[Route("api/v1/logs")]
public sealed class LogBatchController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogger<LogBatchController> _logger;

    public LogBatchController(ILogService logService, ILogger<LogBatchController> logger)
    {
        _logService = logService;
        _logger = logger;
    }

    /// <summary>Ingests batch log events from internal services.</summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> BatchLog([FromBody] object payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[REST Log] Received log batch ingestion request");
        await _logService.IngestBatchAsync(payload, cancellationToken);
        return Accepted(new { success = true, message = "Log batch accepted for processing." });
    }
}
