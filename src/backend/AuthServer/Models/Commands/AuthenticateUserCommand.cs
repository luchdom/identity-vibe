namespace AuthServer.Models.Commands;

public record AuthenticateUserCommand
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public bool RememberMe { get; init; } = false;
}