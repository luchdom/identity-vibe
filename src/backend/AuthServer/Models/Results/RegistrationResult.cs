using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Models.Results;

/// <summary>
/// Type alias for registration operations
/// </summary>
public static class RegistrationResult
{
    public static Result<RegistrationData> Success(AuthenticatedUser user) =>
        Result<RegistrationData>.Success(new RegistrationData { User = user });

    public static Result<RegistrationData> UserAlreadyExists() =>
        Result<RegistrationData>.Failure(CommonErrors.UserAlreadyExists);

    public static Result<RegistrationData> ValidationFailed(string[] errors) =>
        Result<RegistrationData>.Failure("VALIDATION_FAILED", string.Join(", ", errors));

    public static Result<RegistrationData> Failure(string code, string message) =>
        Result<RegistrationData>.Failure(new Error(code, message));
}