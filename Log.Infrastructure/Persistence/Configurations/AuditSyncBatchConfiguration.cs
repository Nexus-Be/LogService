namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Log.Domain.Entities;

public class AuditSyncBatchConfiguration : IEntityTypeConfiguration<AuditSyncBatch>
{
    public void Configure(EntityTypeBuilder<AuditSyncBatch> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FirstRecordId).HasMaxLength(100);
        builder.Property(x => x.LastRecordId).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
    }
}
