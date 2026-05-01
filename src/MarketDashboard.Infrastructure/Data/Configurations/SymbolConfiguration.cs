using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class SymbolConfiguration : IEntityTypeConfiguration<Symbol>
{
    public void Configure(EntityTypeBuilder<Symbol> builder)
    {
        builder.ToTable("Symbols");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();

        builder.Property(s => s.Ticker)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(s => s.Ticker)
            .HasDatabaseName("IX_Symbols_Ticker")
            .IsUnique();

        builder.Property(s => s.CompanyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.HasMany(s => s.Prices)
            .WithOne(p => p.SymbolNavigation)
            .HasForeignKey(p => p.SymbolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.OhlcvRecords)
            .WithOne(o => o.SymbolNavigation)
            .HasForeignKey(o => o.SymbolId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.WatchlistItems)
            .WithOne(w => w.SymbolNavigation)
            .HasForeignKey(w => w.SymbolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.PriceAlerts)
            .WithOne(a => a.SymbolNavigation)
            .HasForeignKey(a => a.SymbolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
