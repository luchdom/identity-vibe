using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data;

public class AppRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}