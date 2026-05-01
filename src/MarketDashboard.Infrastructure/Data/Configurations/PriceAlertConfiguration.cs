using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class PriceAlertConfiguration : IEntityTypeConfiguration<PriceAlert>
{
    public void Configure(EntityTypeBuilder<PriceAlert> builder)
    {
        builder.ToTable("PriceAlerts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.ThresholdPrice)
            .IsRequired()
            .HasColumnType("numeric(18,6)");

        builder.Property(a => a.Direction)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasMany(a => a.History)
            .WithOne(h => h.Alert)
            .HasForeignKey(h => h.AlertId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.UserId, a.IsActive })
            .HasDatabaseName("IX_PriceAlerts_UserId_IsActive");
    }
}
