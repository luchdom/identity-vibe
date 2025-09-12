using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Data.Entities;

namespace Orders.Data.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.HasKey(osh => osh.Id);
        builder.HasIndex(osh => osh.OrderId);
        builder.HasIndex(osh => new { osh.OrderId, osh.CreatedAt });
        
        // Relationships
        builder.HasOne(osh => osh.Order)
              .WithMany(o => o.StatusHistory)
              .HasForeignKey(osh => osh.OrderId)
              .OnDelete(DeleteBehavior.Cascade);
        
        builder.Property(osh => osh.CreatedAt).HasDefaultValueSql("datetime('now')");
    }
}