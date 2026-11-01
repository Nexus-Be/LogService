---
name: controller-conventions
description: Canonical standards for ASP.NET Core thin controllers in LogService (port 8004). Covers boundary validation, Log.Application shared request/response DTOs, English sentence-case error messages, append-only invariants, and the reference controller blueprint.
---

# Controller Conventions & Standards in LogService

> Scope: LogService repository only. Never reference parent workspace paths, root documentation, or sibling service directories.

This skill defines the invariant structural and boundary rules for all REST API controllers in `Log.Api`.

Controllers serve strictly as **HTTP Transport Boundary Adapters**. They bind incoming HTTP protocol details, invoke Log Application use cases, and translate results into standardized HTTP responses. They must never contain business decisions, direct database queries, or manual error formatting.

---

## 0. Append-Only Invariant

LogService enforces a strict audit and telemetry invariant:
- **Zero Mutating or Deletion Actions**: Centralized system logs are append-only. Controllers only expose `GET` (query/filtering) and `POST` (batch ingestion) endpoints.
- **Prohibit PUT, PATCH, DELETE**: Never declare or implement update or delete actions on log entries.
- **Single Official Route**: Never declare duplicate route aliases (e.g. prohibited: adding `[HttpGet("admin/logs")]` alongside `[HttpGet]`). Use only `api/v1/logs`.

---

## 1. When to Use

Consult and follow this skill whenever you:
- Create a new controller or add action methods in `Log.Api/Controllers/Public/`.
- Review controller code changes for architectural consistency in LogService.
- Bind query filter parameters (`[FromQuery]`).
- Design validation placement for incoming client payloads.
- Format outbound responses, pagination metadata, or status codes.

---

## 2. Request and Response DTO Placement (Application Layer Sharing)

To prevent duplicate wrapper classes and maintain clean domain boundaries:

- **Declare DTOs in Log.Application**: All Request and Response records are declared directly in `Log.Application/Services/Log/LogRequests.cs` and `LogResponses.cs`.
- **Direct Consumption by Controller**: Controllers directly bind these Application-layer request records and return these Application-layer response records.
- **Prohibit Duplicate API DTOs**: Never create redundant DTOs in `Log.Api` (e.g. prohibited: creating `ApiQueryLogsRequest` in Api only to map into `QueryLogsRequest` in Application).
- **Domain Entities Stay Private**: Never expose EF Core entities (`LogEntry`, `LogEventType`) through the controller. Only immutable DTO records and `PagedData<T>` leave the application boundary.

---

## 3. Validation Placement (Boundary Discipline)

Validation is concentrated exclusively at system boundaries:

- **Boundary Validation on Request Records**: Place declarative validation attributes from `System.ComponentModel.DataAnnotations` directly on Request DTO records (`[Required]`, `[Range]`, `[RegularExpression]`).
- **Automatic Model State Rejection**: Invalid inputs are intercepted automatically by ASP.NET Core model validation via `ApiValidationProblemFactory`, producing standardized RFC 9110 ProblemDetails with `errorCode = "VAL_INVALID_INPUT"`.
- **PROHIBIT Manual Validation in Controllers**: Never write ad-hoc validation checks inside controller action bodies.

---

## 4. Error Message Standards (Descriptive Sentence Case)

When domain exceptions, validation failures, or query guards trigger errors:

- **100% English Language**: All error messages, failure details, and exception descriptions must be written in English.
- **Sentence Case Format**:
  - Capitalize ONLY the first letter of the sentence.
  - All subsequent words must be lowercase, except technical acronyms and identifiers (`ID`, `OTP`, `JWT`, `URL`, `UUID`, `Email`, `API`, `HTTP`).
  - Prohibit ALL-CAPS messages (e.g. prohibited: `"LOG NOT FOUND"`).
  - Prohibit Title Case (e.g. prohibited: `"Log Not Found"`).
- **Contextual Precision**: The message must precisely describe what condition failed so any developer or client can diagnose the issue immediately.
  - Correct: `"Log entry was not found with the specified identifier."`
  - Correct: `"Event type is currently disabled for ingestion."`
  - Correct: `"Date range 'from' must be earlier than 'to'."`
- **Error Codes**: Always `SCREAMING_SNAKE_CASE` without arbitrary `ERR_` prefixes (`LOG_NOT_FOUND`, `EVENT_TYPE_DISABLED`, `VAL_INVALID_INPUT`, `UNAUTHORIZED`, `FORBIDDEN`).

---

## 5. Canonical Reference Controller Blueprint

The following structural skeleton represents the standardized pattern for controllers in `Log.Api`:

```csharp
namespace Log.Api.Controllers;

using System;
using System.Threading;
using System.Threading.Tasks;
using Log.Api.Filters;
using Log.Application.Common.Models;
using Log.Application.Services.Log;
using Microsoft.AspNetCore.Http;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
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
            logLevel);

        var result = await _logService.QueryLogsAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Gets a single log entry by its unique identifier.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<LogEntryResponse>> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _logService.GetLogByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
```

---

## 6. HTTP Status & Return Conventions

- **GET Single Item**: Return `Task<ActionResult<T>>` with `Ok(dto)` (HTTP 200).
- **GET Paginated List**: Return `Task<ActionResult<PagedData<T>>>` with `Ok(pagedData)` (HTTP 200).
- **POST Ingestion**: Return `Task<IActionResult>` with `StatusCode(StatusCodes.Status201Created, dto)` or `Ok(result)` (HTTP 200).
- **No Manual Outer Envelopes**: Microservice controllers return pure payloads. Wrapping into `ApiResponse<T>` is performed by Gateway `ResponseEnvelopeMiddleware`.
- **Automatic Error Responses**: All unhandled domain exceptions bubble up to `GlobalExceptionHandlerMiddleware` to format RFC 9110 ProblemDetails.
