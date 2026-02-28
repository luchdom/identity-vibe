namespace AuthServer.Models.AdminUsers;

public sealed record UpdateUserStatusRequest
{
    public bool IsActive { get; init; }
}
