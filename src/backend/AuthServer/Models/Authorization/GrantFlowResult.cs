using System.Security.Claims;

namespace AuthServer.Models.Authorization;

/// <summary>
/// Represents a protocol-level outcome for OpenIddict token grant handling.
/// </summary>
public sealed record GrantFlowResult
{
    private GrantFlowResult(ClaimsPrincipal? principal, string? error, string? errorDescription)
    {
        Principal = principal;
        Error = error;
        ErrorDescription = errorDescription;
    }

    public ClaimsPrincipal? Principal { get; }
    public string? Error { get; }
    public string? ErrorDescription { get; }

    public bool IsSuccess => Principal is not null;

    public static GrantFlowResult SignIn(ClaimsPrincipal principal) => new(principal, null, null);

    public static GrantFlowResult Forbid(string error, string? errorDescription = null) =>
        new(null, error, errorDescription);
}
