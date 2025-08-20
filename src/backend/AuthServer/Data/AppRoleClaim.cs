using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class AppRoleClaim : IdentityRoleClaim<string>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}