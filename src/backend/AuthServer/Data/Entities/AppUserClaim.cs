using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Entities;

public class AppUserClaim : IdentityUserClaim<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}