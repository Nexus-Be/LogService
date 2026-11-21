namespace Log.Application.Interfaces;

using Microsoft.EntityFrameworkCore;
using global::Log.Domain.Entities;

public interface IApplicationDbContext
{
    DbSet<LogEntry> Logs { get; }
    DbSet<LogEventType> LogEventTypes { get; }
    DbSet<CentralAuditLog> CentralAuditLogs { get; }
    DbSet<AuditSyncBatch> AuditSyncBatches { get; }
    DbSet<SecurityAuditAlert> SecurityAuditAlerts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
