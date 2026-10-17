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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LogDbContext).Assembly);

        // MassTransit Transactional Outbox entities
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
