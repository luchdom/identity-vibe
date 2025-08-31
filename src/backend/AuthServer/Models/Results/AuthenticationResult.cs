using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Models.Results;

/// <summary>
/// Type alias for authentication operations
/// </summary>
public static class AuthenticationResult
{
    public static Result<AuthenticationData> Success(AuthenticatedUser user, string token, string[] scopes) =>
        Result<AuthenticationData>.Success(new AuthenticationData
        {
            User = user,
            AccessToken = token,
            GrantedScopes = scopes
        });

    public static Result<AuthenticationData> InvalidCredentials() =>
        Result<AuthenticationData>.Failure(CommonErrors.InvalidCredentials);

    public static Result<AuthenticationData> UserNotFound() =>
        Result<AuthenticationData>.Failure(CommonErrors.UserNotFound);

    public static Result<AuthenticationData> Failure(string code, string message) =>
        Result<AuthenticationData>.Failure(new Error(code, message));
}