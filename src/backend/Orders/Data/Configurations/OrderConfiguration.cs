using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Data.Entities;

namespace Orders.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.IdempotencyKey);
        builder.HasIndex(o => new { o.UserId, o.Status });
        
        // Relationships
        builder.HasOne(o => o.Customer)
              .WithMany(c => c.Orders)
              .HasForeignKey(o => o.CustomerId)
              .OnDelete(DeleteBehavior.Restrict);
        
        // Decimal precision
        builder.Property(o => o.SubtotalAmount).HasPrecision(10, 2);
        builder.Property(o => o.TaxAmount).HasPrecision(10, 2);
        builder.Property(o => o.ShippingAmount).HasPrecision(10, 2);
        builder.Property(o => o.DiscountAmount).HasPrecision(10, 2);
        builder.Property(o => o.TotalAmount).HasPrecision(10, 2);
        
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("datetime('now')");
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("datetime('now')");
    }
}