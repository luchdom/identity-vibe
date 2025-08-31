using Microsoft.EntityFrameworkCore;
using Orders.Entities;

namespace Orders.Data;

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistory { get; set; }
    public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Customer configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.Email).IsUnique();
            entity.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(c => c.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Order configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(o => o.Id);
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.IdempotencyKey);
            entity.HasIndex(o => new { o.UserId, o.Status });
            
            // Relationships
            entity.HasOne(o => o.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(o => o.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
            
            // Decimal precision
            entity.Property(o => o.SubtotalAmount).HasPrecision(10, 2);
            entity.Property(o => o.TaxAmount).HasPrecision(10, 2);
            entity.Property(o => o.ShippingAmount).HasPrecision(10, 2);
            entity.Property(o => o.DiscountAmount).HasPrecision(10, 2);
            entity.Property(o => o.TotalAmount).HasPrecision(10, 2);
            
            entity.Property(o => o.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(o => o.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(oi => oi.Id);
            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductSku);
            
            // Relationships
            entity.HasOne(oi => oi.Order)
                  .WithMany(o => o.OrderItems)
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            // Decimal precision
            entity.Property(oi => oi.UnitPrice).HasPrecision(10, 2);
            entity.Property(oi => oi.DiscountAmount).HasPrecision(10, 2);
            entity.Property(oi => oi.LineTotal).HasPrecision(10, 2);
            
            entity.Property(oi => oi.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(oi => oi.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // OrderStatusHistory configuration
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.HasKey(osh => osh.Id);
            entity.HasIndex(osh => osh.OrderId);
            entity.HasIndex(osh => new { osh.OrderId, osh.CreatedAt });
            
            // Relationships
            entity.HasOne(osh => osh.Order)
                  .WithMany(o => o.StatusHistory)
                  .HasForeignKey(osh => osh.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.Property(osh => osh.CreatedAt).HasDefaultValueSql("datetime('now')");
        });

        // IdempotencyRecord configuration
        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasKey(ir => ir.Id);
            entity.HasIndex(ir => ir.IdempotencyKey).IsUnique();
            entity.HasIndex(ir => new { ir.UserId, ir.IdempotencyKey });
            entity.HasIndex(ir => ir.ExpiresAt); // For cleanup queries
            
            entity.Property(ir => ir.CreatedAt).HasDefaultValueSql("datetime('now')");
        });
    }
}