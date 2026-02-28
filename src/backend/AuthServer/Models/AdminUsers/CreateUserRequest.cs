namespace AuthServer.Models.AdminUsers;

public sealed record CreateUserRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; } = true;
    public required string[] RoleNames { get; init; }
}
