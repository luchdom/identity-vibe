using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class AppUserClaim : IdentityUserClaim<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}