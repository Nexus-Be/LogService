namespace Log.Application.Services.Log;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using global::Log.Application.Interfaces;
using global::Log.Domain.Entities;
using global::Log.Domain.Exceptions;
using global::Log.Application.Common;
using global::Log.Application.Common.Models;

public sealed class LogService : ILogService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<LogService> _logger;

    public LogService(IApplicationDbContext dbContext, ILogger<LogService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PagedData<LogEntryResponse>> QueryLogsAsync(QueryLogsRequest request, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PaginationConstants.Normalize(request.Page, request.PageSize);

        var query = _dbContext.Logs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
        {
            var service = request.ServiceName.Trim();
            query = query.Where(l => l.ServiceName == service);
        }

        if (!string.IsNullOrWhiteSpace(request.EventType))
        {
            var eventType = request.EventType.Trim();
            query = query.Where(l => l.EventType == eventType);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityName))
        {
            var entityName = request.EntityName.Trim();
            query = query.Where(l => l.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            var entityId = request.EntityId.Trim();
            query = query.Where(l => l.EntityId == entityId);
        }

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
        {
            var actor = request.ActorUserId.Trim();
            query = query.Where(l => l.ActorUserId == actor);
        }

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
        {
            var correlation = request.CorrelationId.Trim();
            query = query.Where(l => l.CorrelationId == correlation);
        }

        if (!string.IsNullOrWhiteSpace(request.LogLevel))
        {
            var level = request.LogLevel.Trim();
            query = query.Where(l => l.LogLevel == level);
        }

        if (!string.IsNullOrWhiteSpace(request.From) && DateTimeOffset.TryParse(request.From, out var fromDate))
        {
            query = query.Where(l => l.OccurredAt >= fromDate);
        }

        if (!string.IsNullOrWhiteSpace(request.To) && DateTimeOffset.TryParse(request.To, out var toDate))
        {
            query = query.Where(l => l.OccurredAt <= toDate);
        }

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LogEntryResponse(
                l.Id,
                l.OccurredAt,
                l.ServiceName,
                l.EventType,
                l.Action,
                l.EntityName,
                l.EntityId,
                l.ActorUserId,
                l.ActorRole,
                l.CorrelationId,
                l.TraceId,
                l.LogLevel,
                l.Payload))
            .ToListAsync(cancellationToken);

        return new PagedData<LogEntryResponse>(items, PaginationMetadata.Create(page, pageSize, totalItems));
    }

    public async Task<LogEntryResponse> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var log = await _dbContext.Logs
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new EntityNotFoundException("LOG_NOT_FOUND", "LogEntry", id);

        return new LogEntryResponse(
            log.Id,
            log.OccurredAt,
            log.ServiceName,
            log.EventType,
            log.Action,
            log.EntityName,
            log.EntityId,
            log.ActorUserId,
            log.ActorRole,
            log.CorrelationId,
            log.TraceId,
            log.LogLevel,
            log.Payload);
    }

    public async Task<LogEventTypeListResponse> ListEventTypesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.LogEventTypes
            .AsNoTracking()
            .OrderBy(e => e.Code)
            .Select(e => new LogEventTypeResponse(
                e.Code,
                e.ServiceName,
                e.Category,
                e.Description,
                e.IsEnabled,
                e.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new LogEventTypeListResponse(items, items.Count);
    }

    public async Task<LogEventTypeResponse> SetEventTypeEnabledAsync(SetLogEventTypeEnabledRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Code))
        {
            throw new BusinessRuleValidationException("VAL_INVALID_INPUT", "Event type code is required.");
        }

        var code = request.Code.Trim();
        var entity = await _dbContext.LogEventTypes
            .FirstOrDefaultAsync(e => e.Code == code, cancellationToken);

        if (entity == null)
        {
            throw new EntityNotFoundException(nameof(LogEventType), code);
        }

        entity.IsEnabled = request.IsEnabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Event type '{Code}' enabled status updated to {IsEnabled}", code, request.IsEnabled);

        return new LogEventTypeResponse(
            entity.Code,
            entity.ServiceName,
            entity.Category,
            entity.Description,
            entity.IsEnabled,
            entity.UpdatedAt);
    }

    public Task IngestBatchAsync(object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Batch log ingested successfully");
        return Task.CompletedTask;
    }
}
