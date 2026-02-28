using AuthServer.Data.Entities;

namespace AuthServer.Models.ViewModels;

public sealed record UserViewModel
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? UserName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

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
