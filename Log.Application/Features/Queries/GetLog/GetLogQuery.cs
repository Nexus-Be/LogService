namespace Log.Application.Features.Queries.GetLog;

using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Log.Application.Common.Interfaces;
using global::Log.Application.DTOs;

public record GetLogQuery(Guid Id) : IRequest<LogEntryDto?>;

public class GetLogQueryHandler : IRequestHandler<GetLogQuery, LogEntryDto?>
{
    private readonly IApplicationDbContext _context;

    public GetLogQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LogEntryDto?> Handle(GetLogQuery request, CancellationToken cancellationToken)
    {
        var log = await _context.Logs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (log == null)
            return null;

        return new LogEntryDto
        {
            Id = log.Id.ToString(),
            OccurredAt = log.OccurredAt.ToString("o"),
            RecordedAt = log.RecordedAt.ToString("o"),
            ServiceName = log.ServiceName,
            EventType = log.EventType,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            ActorUserId = log.ActorUserId,
            ActorUsername = log.ActorUsername,
            ActorRole = log.ActorRole,
            CorrelationId = log.CorrelationId,
            CausationId = log.CausationId,
            MessageId = log.MessageId,
            SchemaVersion = log.SchemaVersion,
            TraceId = log.TraceId,
            Payload = log.Payload,
            LogLevel = log.LogLevel
        };
    }
}
