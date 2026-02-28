using AuthServer.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthServer.Data.Configurations;

public class AppRoleScopeConfiguration : IEntityTypeConfiguration<AppRoleScope>
{
    public void Configure(EntityTypeBuilder<AppRoleScope> builder)
    {
        builder.ToTable("AspNetRoleScopes");

        builder.HasKey(x => new { x.RoleId, x.ScopeName });

        builder.Property(x => x.RoleId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(x => x.ScopeName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.Role)
            .WithMany(r => r.RoleScopes)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
