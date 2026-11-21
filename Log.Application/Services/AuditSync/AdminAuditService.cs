namespace Log.Application.Services.AuditSync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using global::Log.Application.Common;
using global::Log.Application.Common.Models;
using global::Log.Application.Interfaces;
using global::Log.Domain.Entities;
using global::Log.Domain.Enums;
using global::Log.Domain.Exceptions;

public sealed record AuditLogResponse(
    Guid Id, string ServiceName, string EntityName, string EntityId, string Action, string? UserId, string? UserName,
    string? TraceId, DateTimeOffset CreatedAt, DateTimeOffset SyncedAt, string? OldValues, string? NewValues, string? ChangedColumns);

public sealed record SecurityAlertResponse(
    Guid Id, string Rule, AlertSeverity Severity, AlertStatus Status, string Title, string? Description, string ServiceName,
    Guid AuditLogId, DateTimeOffset CreatedAt, Guid? ResolvedBy, DateTimeOffset? ResolvedAt, string? ResolutionNote);

public sealed record AuditLogFilter(
    int Page, int PageSize, string? ServiceName, string? EntityName, string? EntityId, string? UserId, string? Action,
    string? TraceId, DateTimeOffset? From, DateTimeOffset? To);

public sealed record AlertFilter(int Page, int PageSize, string? Status, string? Severity, string? Rule);

public sealed record UpdateSecurityAlertRequest(string Status, string? Note);

public sealed record ServiceHealthResponse(string ServiceName, string Status, long LatencyMs, DateTimeOffset CheckedAt, string? Message);

public sealed record SystemHealthResponse(string Status, DateTimeOffset CheckedAt, IReadOnlyList<ServiceHealthResponse> Services);

/// <summary>Checks one service. Never throws: a failure is the answer.</summary>
public interface IHealthProbe
{
    IReadOnlyList<string> ServiceNames { get; }

    Task<ServiceHealthResponse> CheckAsync(string service, CancellationToken ct);
}

public sealed class AdminAuditService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditSourceClient _sources;
    private readonly IHealthProbe _health;
    private readonly AuditSyncStateStore _state;
    private readonly AuditSyncOptions _options;

    public AdminAuditService(
        IApplicationDbContext db, IAuditSourceClient sources, IHealthProbe health, AuditSyncStateStore state, IOptions<AuditSyncOptions> options)
    {
        _db = db;
        _sources = sources;
        _health = health;
        _state = state;
        _options = options.Value;
    }

    public async Task<PagedData<AuditLogResponse>> QueryAsync(AuditLogFilter filter, CancellationToken ct)
    {
        var (page, size) = PaginationConstants.Normalize(filter.Page, filter.PageSize);
        var query = _db.CentralAuditLogs.AsNoTracking().Where(c => !c.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.ServiceName)) { var v = filter.ServiceName.Trim(); query = query.Where(c => c.ServiceName == v); }
        if (!string.IsNullOrWhiteSpace(filter.EntityName)) { var v = filter.EntityName.Trim(); query = query.Where(c => c.EntityName == v); }
        if (!string.IsNullOrWhiteSpace(filter.EntityId)) { var v = filter.EntityId.Trim(); query = query.Where(c => c.EntityId == v); }
        if (!string.IsNullOrWhiteSpace(filter.UserId)) { var v = filter.UserId.Trim(); query = query.Where(c => c.UserId == v); }
        if (!string.IsNullOrWhiteSpace(filter.Action)) { var v = filter.Action.Trim(); query = query.Where(c => c.ActionName == v); }
        if (!string.IsNullOrWhiteSpace(filter.TraceId)) { var v = filter.TraceId.Trim(); query = query.Where(c => c.TraceId == v); }
        if (filter.From.HasValue) query = query.Where(c => c.CreatedAt >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(c => c.CreatedAt <= filter.To.Value);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return new PagedData<AuditLogResponse>(rows.Select(Map).ToList(), PaginationMetadata.Create(page, size, total));
    }

    public async Task<AuditLogResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var row = await _db.CentralAuditLogs.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
            ?? throw new DomainException("AUDIT_LOG_NOT_FOUND", "Audit log was not found.", 404);
        return Map(row);
    }

    public async Task<PagedData<SecurityAlertResponse>> QueryAlertsAsync(AlertFilter filter, CancellationToken ct)
    {
        var (page, size) = PaginationConstants.Normalize(filter.Page, filter.PageSize);
        var query = _db.SecurityAuditAlerts.AsNoTracking().Where(a => !a.IsDeleted);

        if (Enum.TryParse<AlertStatus>(filter.Status, true, out var status)) query = query.Where(a => a.Status == status);
        if (Enum.TryParse<AlertSeverity>(filter.Severity, true, out var severity)) query = query.Where(a => a.Severity == severity);
        if (Enum.TryParse<SecurityRuleType>(filter.Rule, true, out var rule)) query = query.Where(a => a.TriggeredRule == rule);

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
            .Skip((page - 1) * size).Take(size).ToListAsync(ct);

        return new PagedData<SecurityAlertResponse>(rows.Select(MapAlert).ToList(), PaginationMetadata.Create(page, size, total));
    }

    public async Task<SecurityAlertResponse> UpdateAlertAsync(Guid id, UpdateSecurityAlertRequest request, Guid adminId, CancellationToken ct)
    {
        var alert = await _db.SecurityAuditAlerts.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new DomainException("SECURITY_ALERT_NOT_FOUND", "Security alert was not found.", 404);

        if (!Enum.TryParse<AlertStatus>(request.Status, true, out var next) || next == AlertStatus.Open)
        {
            throw new DomainException("VAL_INVALID_INPUT", "Status must be Investigating, Resolved or Dismissed.", 400);
        }

        if ((next is AlertStatus.Resolved or AlertStatus.Dismissed) && string.IsNullOrWhiteSpace(request.Note))
        {
            throw new DomainException("VAL_INVALID_INPUT", "A note is required to close a security alert.", 400);
        }

        if (alert.Status is AlertStatus.Resolved or AlertStatus.Dismissed)
        {
            throw new DomainException("ALERT_ALREADY_CLOSED", "Security alert is already closed.", 409);
        }

        alert.Status = next;
        alert.ResolutionNote = request.Note?.Trim();
        if (next is AlertStatus.Resolved or AlertStatus.Dismissed)
        {
            alert.ResolvedBy = adminId;
            alert.ResolvedAt = DateTimeOffset.UtcNow;
        }

        alert.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return MapAlert(alert);
    }

    /// <summary>Asks every source how many records it still holds, so the answer is current even when syncing is off.</summary>
    public async Task<AuditSyncStatusResponse> GetSyncStatusAsync(CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(4));
        await Task.WhenAll(_sources.SourceNames.Select(async name =>
        {
            try
            {
                var page = await _sources.GetUnsyncedAsync(name, 1, timeout.Token);
                _state.RecordSuccess(name, page.UnsyncedCount, synced: false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                _state.RecordFailure(name, ex.Message);
            }
        }));

        return new AuditSyncStatusResponse(_options.Enabled, _state.LastBatchAt, _sources.SourceNames.Select(_state.Get).ToList());
    }

    public async Task<SystemHealthResponse> GetHealthAsync(CancellationToken ct)
    {
        var results = await Task.WhenAll(_health.ServiceNames.Select(name => _health.CheckAsync(name, ct)));
        var unhealthy = results.Count(r => r.Status == "Unhealthy");
        var degraded = results.Count(r => r.Status == "Degraded");
        var overall = unhealthy == 0 && degraded == 0 ? "Healthy"
            : unhealthy == results.Length ? "Unhealthy" : "Degraded";
        return new SystemHealthResponse(overall, DateTimeOffset.UtcNow, results);
    }

    private static AuditLogResponse Map(CentralAuditLog c) => new(
        c.Id, c.ServiceName, c.EntityName, c.EntityId, c.ActionName ?? c.Action.ToString(), c.UserId, c.UserName,
        c.TraceId, c.CreatedAt, c.SyncedAt, c.OldValues, c.NewValues, c.ChangedColumns);

    private static SecurityAlertResponse MapAlert(SecurityAuditAlert a) => new(
        a.Id, a.TriggeredRule.ToString(), a.Severity, a.Status, a.AlertTitle, a.Description, a.ServiceName,
        a.CentralAuditLogId, a.CreatedAt, a.ResolvedBy, a.ResolvedAt, a.ResolutionNote);
}
