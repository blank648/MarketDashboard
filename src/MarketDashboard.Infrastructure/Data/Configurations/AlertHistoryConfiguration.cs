using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class AlertHistoryConfiguration : IEntityTypeConfiguration<AlertHistory>
{
    public void Configure(EntityTypeBuilder<AlertHistory> builder)
    {
        builder.ToTable("AlertHistory");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();

        builder.Property(h => h.AlertId).IsRequired();

        builder.Property(h => h.TriggeredAt)
            .IsRequired();

        builder.Property(h => h.PriceAtTrigger)
            .IsRequired()
            .HasColumnType("numeric(18,6)");

        builder.HasIndex(h => h.TriggeredAt)
            .HasDatabaseName("IX_AlertHistory_TriggeredAt");
    }
}
