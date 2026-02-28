namespace AuthServer.Data.Entities;

public class AppRoleScope
{
    public string RoleId { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppRole Role { get; set; } = null!;
}
