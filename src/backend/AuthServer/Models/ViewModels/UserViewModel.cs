namespace AuthServer.Models.ViewModels;

public record UserViewModel
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public bool EmailConfirmed { get; init; }
    public string? PhoneNumber { get; init; }
    public bool PhoneNumberConfirmed { get; init; }
    public bool TwoFactorEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public bool LockoutEnabled { get; init; }
    public int AccessFailedCount { get; init; }
}