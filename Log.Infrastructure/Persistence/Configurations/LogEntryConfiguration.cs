namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using global::Log.Domain.Entities;

public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        builder.ToTable("logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OccurredAt)
            .IsRequired();

        builder.Property(x => x.RecordedAt)
            .IsRequired();

        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.ServiceName);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.CorrelationId);

        builder.Property(x => x.ServiceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Action)
            .HasMaxLength(100);

        builder.Property(x => x.EntityName)
            .HasMaxLength(100);

        builder.Property(x => x.EntityId)
            .HasMaxLength(100);

        builder.Property(x => x.ActorUserId)
            .HasMaxLength(100);

        builder.Property(x => x.ActorUsername)
            .HasMaxLength(100);

        builder.Property(x => x.ActorRole)
            .HasMaxLength(50);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.CausationId)
            .HasMaxLength(100);

        builder.Property(x => x.MessageId)
            .HasMaxLength(100);

        builder.Property(x => x.TraceId)
            .HasMaxLength(100);

        builder.Property(x => x.LogLevel)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("INFO");

        builder.Property(x => x.Payload)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");
    }
}
