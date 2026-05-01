using MarketDashboard.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketDashboard.Infrastructure.Data.Configurations;

public class OhlcvRecordConfiguration : IEntityTypeConfiguration<OhlcvRecord>
{
    public void Configure(EntityTypeBuilder<OhlcvRecord> builder)
    {
        builder.ToTable("OhlcvRecords");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.Open).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.High).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.Low).HasColumnType("numeric(18,6)").IsRequired();
        builder.Property(o => o.Close).HasColumnType("numeric(18,6)").IsRequired();

        builder.Property(o => o.Volume).IsRequired();
        builder.Property(o => o.PeriodStart).IsRequired();
        builder.Property(o => o.PeriodEnd).IsRequired();

        builder.HasIndex(o => new { o.Symbol, o.PeriodStart })
            .HasDatabaseName("IX_OhlcvRecords_Symbol_PeriodStart")
            .IsUnique();
    }
}
