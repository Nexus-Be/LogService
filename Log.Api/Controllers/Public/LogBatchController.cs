namespace Log.Api.Controllers.Public;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Log.Domain.Exceptions;

/// <summary>
/// Retired. Batch ingestion used to answer 202 and then do nothing with the payload, which looked like success while every record was dropped.
/// Services keep their own audit logs and LogService pulls them in batches (AuditSync), so there is nothing to push here.
/// </summary>
[ApiController]
[Route("api/v1/logs")]
public sealed class LogBatchController : ControllerBase
{
    public const string RetiredErrorCode = "LOG_BATCH_INGEST_RETIRED";

    /// <summary>Always answers 410 Gone so a caller that still pushes batches finds out instead of losing data silently.</summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public IActionResult BatchLog() =>
        throw new DomainException(
            RetiredErrorCode,
            "Log batch ingestion is no longer supported. Services keep their own audit logs and the log service pulls them.",
            StatusCodes.Status410Gone);
}
