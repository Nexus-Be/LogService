namespace Log.Api.Controllers.Public;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Log.Api.Extensions;
using Log.Api.Filters;
using Log.Application.Services.AuditSync;

/// <summary>Central audit log, security alerts and system health for administrators.</summary>
[ApiController]
[Route("api/v1/admin")]
public sealed class AdminAuditController : ControllerBase
{
    private readonly AdminAuditService _service;

    public AdminAuditController(AdminAuditService service)
    {
        _service = service;
    }

    /// <summary>Lists audit records collected from all services.</summary>
    [HttpGet("audit-logs")]
    [RequireRole("admin", "staff")]
    public async Task<IActionResult> QueryAuditLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? serviceName = null, [FromQuery] string? entityName = null, [FromQuery] string? entityId = null,
        [FromQuery] string? userId = null, [FromQuery] string? action = null, [FromQuery] string? traceId = null,
        [FromQuery] DateTimeOffset? fromDate = null, [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken ct = default)
    {
        var filter = new AuditLogFilter(page, pageSize, serviceName, entityName, entityId, userId, action, traceId, fromDate, toDate);
        return Ok(await _service.QueryAsync(filter, ct));
    }

    /// <summary>Shows how far each service is from being fully synced.</summary>
    [HttpGet("audit-logs/sync-status")]
    [RequireRole("admin", "staff")]
    public async Task<IActionResult> GetSyncStatus(CancellationToken ct = default) => Ok(await _service.GetSyncStatusAsync(ct));

    /// <summary>Gets one audit record.</summary>
    [HttpGet("audit-logs/{id:guid}")]
    [RequireRole("admin", "staff")]
    public async Task<IActionResult> GetAuditLog([FromRoute] Guid id, CancellationToken ct = default) => Ok(await _service.GetAsync(id, ct));

    /// <summary>Lists security alerts.</summary>
    [HttpGet("security-alerts")]
    [RequireRole("admin", "staff")]
    public async Task<IActionResult> QueryAlerts(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null, [FromQuery] string? severity = null, [FromQuery] string? rule = null,
        CancellationToken ct = default)
        => Ok(await _service.QueryAlertsAsync(new AlertFilter(page, pageSize, status, severity, rule), ct));

    /// <summary>Closes or updates a security alert. Administrators only; closing needs a note.</summary>
    [HttpPatch("security-alerts/{id:guid}")]
    [RequireAdminOnly]
    public async Task<IActionResult> UpdateAlert([FromRoute] Guid id, [FromBody] UpdateSecurityAlertRequest request, CancellationToken ct = default)
    {
        var adminId = HttpContext.GetUserId() ?? Guid.Empty;
        return Ok(await _service.UpdateAlertAsync(id, request, adminId, ct));
    }

    /// <summary>Reports the health of every service.</summary>
    [HttpGet("system-health")]
    [RequireRole("admin", "staff")]
    public async Task<IActionResult> GetSystemHealth(CancellationToken ct = default) => Ok(await _service.GetHealthAsync(ct));
}
