namespace Log.Application.Common.Interfaces;

using Microsoft.EntityFrameworkCore;
using global::Log.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<LogEntry> Logs { get; }
    DbSet<LogEventType> LogEventTypes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
