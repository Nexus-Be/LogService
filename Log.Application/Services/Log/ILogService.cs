namespace Log.Application.Services.Log;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::Log.Application.Common.Models;

public interface ILogService
{
    Task<PagedData<LogEntryResponse>> QueryLogsAsync(QueryLogsRequest request, CancellationToken cancellationToken = default);
    Task<LogEntryResponse> GetLogByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LogEventTypeListResponse> ListEventTypesAsync(CancellationToken cancellationToken = default);
    Task<LogEventTypeResponse> SetEventTypeEnabledAsync(SetLogEventTypeEnabledRequest request, CancellationToken cancellationToken = default);
    Task IngestBatchAsync(object payload, CancellationToken cancellationToken = default);
}
