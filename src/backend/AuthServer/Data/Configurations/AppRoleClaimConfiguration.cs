using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AuthServer.Data.Entities;

namespace AuthServer.Data.Configurations;

public class AppRoleClaimConfiguration : IEntityTypeConfiguration<AppRoleClaim>
{
    public void Configure(EntityTypeBuilder<AppRoleClaim> builder)
    {
        builder.Property(e => e.CreatedBy).HasMaxLength(256);
    }
}