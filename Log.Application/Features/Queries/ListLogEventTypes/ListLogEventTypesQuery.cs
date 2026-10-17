namespace Log.Application.Features.Queries.ListLogEventTypes;

using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Log.Application.Common.Interfaces;
using global::Log.Application.DTOs;

public record ListLogEventTypesQuery : IRequest<LogEventTypeListDto>;

public class ListLogEventTypesQueryHandler : IRequestHandler<ListLogEventTypesQuery, LogEventTypeListDto>
{
    private readonly IApplicationDbContext _context;

    public ListLogEventTypesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LogEventTypeListDto> Handle(ListLogEventTypesQuery request, CancellationToken cancellationToken)
    {
        var items = await _context.LogEventTypes
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new LogEventTypeDto
            {
                Code = x.Code,
                ServiceName = x.ServiceName,
                Category = x.Category,
                Description = x.Description,
                IsEnabled = x.IsEnabled
            })
            .ToListAsync(cancellationToken);

        return new LogEventTypeListDto
        {
            Items = items,
            Total = items.Count
        };
    }
}
