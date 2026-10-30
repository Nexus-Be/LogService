namespace Log.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using global::Log.Application.Common.Interfaces;
using global::Log.Domain.Entities;

public class LogDbContext : DbContext, IApplicationDbContext
{
    public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }

    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<LogEventType> LogEventTypes => Set<LogEventType>();

    public DbSet<CentralAuditLog> CentralAuditLogs => Set<CentralAuditLog>();
    public DbSet<AuditSyncBatch> AuditSyncBatches => Set<AuditSyncBatch>();
    public DbSet<AppLogFileCatalog> AppLogFileCatalogs => Set<AppLogFileCatalog>();
    public DbSet<SecurityAuditAlert> SecurityAuditAlerts => Set<SecurityAuditAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogDbContext).Assembly);
        Seeds.LogSeedData.SeedAll(modelBuilder);
    }
}
