namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Log.Domain.Entities;

public class AppLogFileCatalogConfiguration : IEntityTypeConfiguration<AppLogFileCatalog>
{
    public void Configure(EntityTypeBuilder<AppLogFileCatalog> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.Checksum).HasMaxLength(100);
        builder.HasIndex(x => new { x.ServiceName, x.LogDate }).IsUnique();
    }
}
