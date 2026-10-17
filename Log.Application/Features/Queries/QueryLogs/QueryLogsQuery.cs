namespace Log.Application.Features.Queries.QueryLogs;

using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Log.Application.Common.Interfaces;
using global::Log.Application.DTOs;

public record QueryLogsQuery(
    int Page,
    int PageSize,
    string? From,
    string? To,
    string? ServiceName,
    string? EventType,
    string? EntityName,
    string? EntityId,
    string? ActorUserId,
    string? CorrelationId,
    string? LogLevel
) : IRequest<LogPageDto>;

public class QueryLogsQueryHandler : IRequestHandler<QueryLogsQuery, LogPageDto>
{
    private readonly IApplicationDbContext _context;

    public QueryLogsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LogPageDto> Handle(QueryLogsQuery request, CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);

        var query = _context.Logs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.ServiceName))
            query = query.Where(x => x.ServiceName == request.ServiceName.Trim());

        if (!string.IsNullOrWhiteSpace(request.EventType))
            query = query.Where(x => x.EventType == request.EventType.Trim());

        if (!string.IsNullOrWhiteSpace(request.EntityName))
            query = query.Where(x => x.EntityName == request.EntityName.Trim());

        if (!string.IsNullOrWhiteSpace(request.EntityId))
            query = query.Where(x => x.EntityId == request.EntityId.Trim());

        if (!string.IsNullOrWhiteSpace(request.ActorUserId))
            query = query.Where(x => x.ActorUserId == request.ActorUserId.Trim());

        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            query = query.Where(x => x.CorrelationId == request.CorrelationId.Trim());

        if (!string.IsNullOrWhiteSpace(request.LogLevel))
            query = query.Where(x => x.LogLevel == request.LogLevel.Trim().ToUpperInvariant());

        if (DateTimeOffset.TryParse(request.From, out var fromDate))
            query = query.Where(x => x.OccurredAt >= fromDate);

        if (DateTimeOffset.TryParse(request.To, out var toDate))
            query = query.Where(x => x.OccurredAt <= toDate);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new LogEntryDto
            {
                Id = x.Id.ToString(),
                OccurredAt = x.OccurredAt.ToString("o"),
                RecordedAt = x.RecordedAt.ToString("o"),
                ServiceName = x.ServiceName,
                EventType = x.EventType,
                Action = x.Action,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                ActorUserId = x.ActorUserId,
                ActorUsername = x.ActorUsername,
                ActorRole = x.ActorRole,
                CorrelationId = x.CorrelationId,
                CausationId = x.CausationId,
                MessageId = x.MessageId,
                SchemaVersion = x.SchemaVersion,
                TraceId = x.TraceId,
                Payload = string.Empty, // Omitting payload in list queries for lightweight responses
                LogLevel = x.LogLevel
            })
            .ToListAsync(cancellationToken);

        return new LogPageDto
        {
            Items = items,
            Total = total
        };
    }
}
