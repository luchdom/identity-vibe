namespace AuthServer.Models;

public record AccountResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public List<string> Errors { get; init; } = [];
};