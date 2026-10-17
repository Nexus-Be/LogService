namespace Log.Application.Features.Commands.SetLogEventTypeEnabled;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using global::Log.Application.Common.Interfaces;
using global::Log.Application.DTOs;
using global::Log.Domain.Entities;

public record SetLogEventTypeEnabledCommand(string Code, bool Enabled) : IRequest<LogEventTypeDto>;

public class SetLogEventTypeEnabledCommandValidator : AbstractValidator<SetLogEventTypeEnabledCommand>
{
    public SetLogEventTypeEnabledCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().WithMessage("Mã sự kiện (Code) không được để trống.");
    }
}

public class SetLogEventTypeEnabledCommandHandler : IRequestHandler<SetLogEventTypeEnabledCommand, LogEventTypeDto>
{
    private readonly IApplicationDbContext _context;

    public SetLogEventTypeEnabledCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LogEventTypeDto> Handle(SetLogEventTypeEnabledCommand request, CancellationToken cancellationToken)
    {
        var eventType = await _context.LogEventTypes
            .FirstOrDefaultAsync(x => x.Code == request.Code, cancellationToken);

        if (eventType == null)
        {
            eventType = new LogEventType
            {
                Code = request.Code,
                ServiceName = "system",
                Category = "custom",
                Description = $"Tự động sinh cấu hình cho {request.Code}",
                IsEnabled = request.Enabled,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            _context.LogEventTypes.Add(eventType);
        }
        else
        {
            eventType.IsEnabled = request.Enabled;
            eventType.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new LogEventTypeDto
        {
            Code = eventType.Code,
            ServiceName = eventType.ServiceName,
            Category = eventType.Category,
            Description = eventType.Description,
            IsEnabled = eventType.IsEnabled
        };
    }
}
