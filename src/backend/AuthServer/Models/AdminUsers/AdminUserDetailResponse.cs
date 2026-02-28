namespace AuthServer.Models.AdminUsers;

public sealed record AdminUserDetailResponse
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public required string[] Roles { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
