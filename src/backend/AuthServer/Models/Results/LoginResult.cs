using AuthServer.Models.Results;

namespace AuthServer.Models.Results;

public sealed record LoginResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UserResult? User { get; init; }
}
