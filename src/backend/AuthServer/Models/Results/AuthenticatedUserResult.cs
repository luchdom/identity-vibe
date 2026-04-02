namespace AuthServer.Models.Results;

public sealed record AuthenticatedUserResult
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string[] Roles { get; init; } = [];
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
}
