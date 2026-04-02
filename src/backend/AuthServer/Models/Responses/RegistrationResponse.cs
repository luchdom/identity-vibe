using AuthServer.Models.Results;

namespace AuthServer.Models.Responses;

public record RegistrationResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public AuthenticatedUserResult? User { get; init; }
    public List<string> Errors { get; init; } = [];
}