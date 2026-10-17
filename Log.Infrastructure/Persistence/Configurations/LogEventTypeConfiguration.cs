namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using global::Log.Domain.Entities;

public class LogEventTypeConfiguration : IEntityTypeConfiguration<LogEventType>
{
    public void Configure(EntityTypeBuilder<LogEventType> builder)
    {
        builder.ToTable("log_event_types");

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ServiceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Category)
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();
    }
}
