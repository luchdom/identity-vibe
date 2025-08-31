namespace Gateway.Models.Requests;

public record RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}