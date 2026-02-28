namespace AuthServer.Models.Commands;

public sealed record RegisterCommand
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string ConfirmPassword { get; init; }
}
