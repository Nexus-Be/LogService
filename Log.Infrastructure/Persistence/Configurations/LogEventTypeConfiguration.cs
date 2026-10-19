namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Log.Domain.Entities;

public class LogEventTypeConfiguration : IEntityTypeConfiguration<LogEventType>
{
    public void Configure(EntityTypeBuilder<LogEventType> builder)
    {
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
