using Microsoft.AspNetCore.Identity;

namespace AuthServer.Entities;

public class Role : IdentityRole
{
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}