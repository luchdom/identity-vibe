namespace AuthServer.Models.AdminUsers;

public sealed record ResetUserPasswordRequest
{
    public required string NewPassword { get; init; }
}
