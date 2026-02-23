using AuthServer.Data.Entities;

namespace AuthServer.Models.ViewModels;

public class UserViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public static UserViewModel FromEntity(AppUser user)
    {
        return new UserViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
