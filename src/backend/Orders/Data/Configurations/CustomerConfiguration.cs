using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orders.Data.Entities;

namespace Orders.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Email).IsUnique();
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("datetime('now')");
    }
}