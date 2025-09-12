using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Data.Entities;

namespace Orders.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(oi => oi.Id);
        builder.HasIndex(oi => oi.OrderId);
        builder.HasIndex(oi => oi.ProductSku);
        
        // Relationships
        builder.HasOne(oi => oi.Order)
              .WithMany(o => o.OrderItems)
              .HasForeignKey(oi => oi.OrderId)
              .OnDelete(DeleteBehavior.Cascade);
        
        // Decimal precision
        builder.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
        builder.Property(oi => oi.DiscountAmount).HasPrecision(10, 2);
        builder.Property(oi => oi.LineTotal).HasPrecision(10, 2);
        
        builder.Property(oi => oi.CreatedAt).HasDefaultValueSql("datetime('now')");
        builder.Property(oi => oi.UpdatedAt).HasDefaultValueSql("datetime('now')");
    }
}