namespace Log.Infrastructure.Persistence;

using MassTransit;
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

        // MassTransit Transactional Outbox entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

public class AppDbContext : LogDbContext
{
    public AppDbContext(DbContextOptions<LogDbContext> options) : base(options)
    {
    }
}
