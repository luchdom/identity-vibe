namespace Gateway.Models.Responses;

public record AuthResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public string? AccessToken { get; init; }
    public object? User { get; init; }
    public List<string> Errors { get; init; } = [];
}