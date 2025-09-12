using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Data.Entities;

namespace Orders.Data.Configurations;

public class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.HasKey(ir => ir.Id);
        builder.HasIndex(ir => ir.IdempotencyKey).IsUnique();
        builder.HasIndex(ir => new { ir.UserId, ir.IdempotencyKey });
        builder.HasIndex(ir => ir.ExpiresAt); // For cleanup queries
        
        builder.Property(ir => ir.CreatedAt).HasDefaultValueSql("datetime('now')");
    }
}