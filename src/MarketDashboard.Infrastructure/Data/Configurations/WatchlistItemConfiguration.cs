using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class WatchlistItemConfiguration : IEntityTypeConfiguration<WatchlistItem>
{
    public void Configure(EntityTypeBuilder<WatchlistItem> builder)
    {
        builder.ToTable("WatchlistItems");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedOnAdd();

        builder.Property(w => w.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(w => w.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(w => w.AddedAt)
            .IsRequired();

        builder.HasIndex(w => new { w.UserId, w.Symbol })
            .HasDatabaseName("IX_WatchlistItems_UserId_Symbol")
            .IsUnique();
    }
}
