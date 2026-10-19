namespace Log.Infrastructure.Persistence.Configurations;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Log.Domain.Entities;

public class SecurityAuditAlertConfiguration : IEntityTypeConfiguration<SecurityAuditAlert>
{
    public void Configure(EntityTypeBuilder<SecurityAuditAlert> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AlertTitle).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>();
        builder.Property(x => x.ServiceName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TriggeredRule).HasConversion<string>();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>();
        builder.Property(x => x.ResolutionNote).HasMaxLength(2000);

        builder.HasOne(a => a.CentralAuditLog)
            .WithMany(c => c.SecurityAlerts)
            .HasForeignKey(a => a.CentralAuditLogId);
    }
}
