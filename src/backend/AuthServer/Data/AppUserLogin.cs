using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class AppUserLogin : IdentityUserLogin<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;
}