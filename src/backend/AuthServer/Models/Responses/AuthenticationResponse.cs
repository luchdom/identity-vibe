using AuthServer.Models.ViewModels;

namespace AuthServer.Models.Responses;

public record AuthenticationResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public AuthenticatedUser? User { get; init; }
    public string? AccessToken { get; init; }
    public string[]? GrantedScopes { get; init; }
    public List<string> Errors { get; init; } = [];
}