using AuthServer.Data.Entities;

namespace AuthServer.Models.Results;

public sealed record UserResult
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? UserName { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public static UserResult FromEntity(AppUser user)
    {
        return new UserResult
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
