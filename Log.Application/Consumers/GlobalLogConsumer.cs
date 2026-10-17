namespace Log.Application.Consumers;

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using global::Log.Application.Common.Interfaces;
using global::Log.Domain.Entities;
using global::Log.Domain.Events;

public class GlobalLogConsumer : IConsumer<ILogEventOccurred>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GlobalLogConsumer> _logger;

    public GlobalLogConsumer(IApplicationDbContext context, ILogger<GlobalLogConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ILogEventOccurred> context)
    {
        var msg = context.Message;

        // Check if event type is enabled
        var config = await _context.LogEventTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == msg.EventType, context.CancellationToken);

        if (config != null && !config.IsEnabled)
        {
            _logger.LogDebug("Bỏ qua log cho event {EventType} vì IsEnabled = false", msg.EventType);
            return;
        }

        var entry = new LogEntry
        {
            OccurredAt = DateTimeOffset.UtcNow,
            RecordedAt = DateTimeOffset.UtcNow,
            ServiceName = msg.ServiceName,
            EventType = msg.EventType,
            Action = msg.EventType,
            EntityName = msg.EntityName,
            EntityId = msg.EntityId,
            ActorUserId = msg.ActorUserId,
            CorrelationId = context.CorrelationId?.ToString() ?? string.Empty,
            MessageId = context.MessageId?.ToString() ?? string.Empty,
            TraceId = System.Diagnostics.Activity.Current?.TraceId.ToString() ?? string.Empty,
            Payload = string.IsNullOrWhiteSpace(msg.DetailsJson) ? "{}" : msg.DetailsJson,
            LogLevel = string.IsNullOrWhiteSpace(msg.LogLevel) ? "INFO" : msg.LogLevel
        };

        _context.Logs.Add(entry);
        await _context.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation("Đã lưu log: {ServiceName} - {EventType} - {EntityName}:{EntityId}",
            msg.ServiceName, msg.EventType, msg.EntityName, msg.EntityId);
    }
}
