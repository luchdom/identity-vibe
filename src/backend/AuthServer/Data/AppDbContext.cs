using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Data;

public class AppDbContext : IdentityDbContext<
    AppUser,
    AppRole,
    string,
    AppUserClaim,
    AppUserRole,
    AppUserLogin,
    AppRoleClaim,
    AppUserToken>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure OpenIddict to use the default entity key type (string)
        builder.UseOpenIddict();

        // Configure entity relationships and constraints
        builder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
        });

        builder.Entity<AppRole>(entity =>
        {
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        builder.Entity<AppUserRole>(entity =>
        {
            entity.Property(e => e.AssignedBy).HasMaxLength(256);
        });

        builder.Entity<AppRoleClaim>(entity =>
        {
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
        });
    }
} 