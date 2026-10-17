namespace Log.Api.Services;

using Grpc.Core;
using MediatR;
using ShoppingCart.Contracts.V1;
using global::Log.Application.Features.Commands.SetLogEventTypeEnabled;
using global::Log.Application.Features.Queries.GetLog;
using global::Log.Application.Features.Queries.ListLogEventTypes;
using global::Log.Application.Features.Queries.QueryLogs;

public class LogGrpcService : LogApi.LogApiBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogGrpcService> _logger;

    public LogGrpcService(IMediator mediator, ILogger<LogGrpcService> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task<LogPage> QueryLogs(QueryLogsRequest request, ServerCallContext context)
    {
        var result = await _mediator.Send(new QueryLogsQuery(
            request.Page,
            request.PageSize,
            request.From,
            request.To,
            request.ServiceName,
            request.EventType,
            request.EntityName,
            request.EntityId,
            request.ActorUserId,
            request.CorrelationId,
            request.LogLevel
        ), context.CancellationToken);

        var page = new LogPage { Total = result.Total };
        foreach (var item in result.Items)
        {
            page.Items.Add(new LogEntry
            {
                Id = item.Id,
                OccurredAt = item.OccurredAt,
                RecordedAt = item.RecordedAt,
                ServiceName = item.ServiceName,
                EventType = item.EventType,
                Action = item.Action,
                EntityName = item.EntityName,
                EntityId = item.EntityId,
                ActorUserId = item.ActorUserId,
                ActorUsername = item.ActorUsername,
                ActorRole = item.ActorRole,
                CorrelationId = item.CorrelationId,
                CausationId = item.CausationId,
                MessageId = item.MessageId,
                SchemaVersion = item.SchemaVersion,
                TraceId = item.TraceId,
                Payload = item.Payload,
                LogLevel = item.LogLevel
            });
        }

        return page;
    }

    public override async Task<LogEntry> GetLog(IdRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ID nhật ký không hợp lệ"));

        var item = await _mediator.Send(new GetLogQuery(id), context.CancellationToken);
        if (item == null)
            throw new RpcException(new Status(StatusCode.NotFound, "Không tìm thấy nhật ký tương ứng"));

        return new LogEntry
        {
            Id = item.Id,
            OccurredAt = item.OccurredAt,
            RecordedAt = item.RecordedAt,
            ServiceName = item.ServiceName,
            EventType = item.EventType,
            Action = item.Action,
            EntityName = item.EntityName,
            EntityId = item.EntityId,
            ActorUserId = item.ActorUserId,
            ActorUsername = item.ActorUsername,
            ActorRole = item.ActorRole,
            CorrelationId = item.CorrelationId,
            CausationId = item.CausationId,
            MessageId = item.MessageId,
            SchemaVersion = item.SchemaVersion,
            TraceId = item.TraceId,
            Payload = item.Payload,
            LogLevel = item.LogLevel
        };
    }

    public override async Task<LogEventTypeList> ListLogEventTypes(Empty request, ServerCallContext context)
    {
        var result = await _mediator.Send(new ListLogEventTypesQuery(), context.CancellationToken);
        var list = new LogEventTypeList { Total = result.Total };

        foreach (var item in result.Items)
        {
            list.Items.Add(new LogEventType
            {
                Code = item.Code,
                ServiceName = item.ServiceName,
                Category = item.Category,
                Description = item.Description,
                IsEnabled = item.IsEnabled
            });
        }

        return list;
    }

    public override async Task<LogEventType> SetLogEventTypeEnabled(SetLogEventTypeEnabledRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Code không được để trống"));

        var item = await _mediator.Send(new SetLogEventTypeEnabledCommand(request.Code, request.Enabled), context.CancellationToken);

        return new LogEventType
        {
            Code = item.Code,
            ServiceName = item.ServiceName,
            Category = item.Category,
            Description = item.Description,
            IsEnabled = item.IsEnabled
        };
    }
}
