using AuthServer.Models.ViewModels;

namespace AuthServer.Models.Responses;

public record RegistrationResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public AuthenticatedUserViewModel? User { get; init; }
    public List<string> Errors { get; init; } = [];
}