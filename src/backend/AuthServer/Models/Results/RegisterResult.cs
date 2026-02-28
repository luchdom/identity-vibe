using AuthServer.Models.ViewModels;

namespace AuthServer.Models.Results;

public sealed record RegisterResult
{
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public UserViewModel? User { get; init; }
}
