namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Log.Domain.Entities;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EventType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(100);
        builder.Property(x => x.EntityName).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.Property(x => x.ActorUserId).HasMaxLength(100);
        builder.Property(x => x.ActorUsername).HasMaxLength(100);
        builder.Property(x => x.ActorRole).HasMaxLength(50);
        builder.Property(x => x.CorrelationId).HasMaxLength(100);
        builder.Property(x => x.CausationId).HasMaxLength(100);
        builder.Property(x => x.MessageId).HasMaxLength(100);
        builder.Property(x => x.TraceId).HasMaxLength(100);
        builder.Property(x => x.LogLevel).HasMaxLength(20);
        builder.Property(x => x.Payload).HasColumnType("jsonb");
    }
}
