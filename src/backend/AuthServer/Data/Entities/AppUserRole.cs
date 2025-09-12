using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Entities;

public class AppUserRole : IdentityUserRole<string>
{
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? AssignedBy { get; set; }
}