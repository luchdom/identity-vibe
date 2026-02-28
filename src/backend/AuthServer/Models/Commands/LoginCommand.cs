namespace AuthServer.Models.Commands;

public sealed record LoginCommand
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; }
}
