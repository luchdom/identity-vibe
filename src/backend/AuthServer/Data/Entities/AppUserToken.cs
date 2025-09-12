using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Entities;

public class AppUserToken : IdentityUserToken<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}