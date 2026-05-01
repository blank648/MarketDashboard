using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class MarketPriceConfiguration : IEntityTypeConfiguration<MarketPrice>
{
    public void Configure(EntityTypeBuilder<MarketPrice> builder)
    {
        builder.ToTable("MarketPrices");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("numeric(18,6)");

        builder.Property(p => p.Volume)
            .IsRequired();

        builder.Property(p => p.RecordedAt)
            .IsRequired();

        builder.Property(p => p.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.HasIndex(p => new { p.Symbol, p.RecordedAt })
            .HasDatabaseName("IX_MarketPrices_Symbol_RecordedAt")
            .IsDescending(false, true);
    }
}
