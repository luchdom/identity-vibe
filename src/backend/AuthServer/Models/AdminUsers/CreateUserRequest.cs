namespace AuthServer.Models.AdminUsers;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string? PhoneNumber,
    bool IsActive,
    string[] RoleNames);
