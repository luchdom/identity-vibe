namespace AuthServer.Models.AdminUsers;

public sealed record UpdateUserRequest
{
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}
