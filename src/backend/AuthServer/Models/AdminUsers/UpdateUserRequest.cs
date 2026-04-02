namespace AuthServer.Models.AdminUsers;

public sealed record UpdateUserRequest(string? Email, string? PhoneNumber);
