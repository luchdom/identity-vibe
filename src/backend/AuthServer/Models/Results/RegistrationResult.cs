using AuthServer.Models.ViewModels;
using Shared.Common;

namespace AuthServer.Models.Results;

/// <summary>
/// Type alias for registration operations
/// </summary>
public static class RegistrationResult
{
    public static Result<RegistrationDataViewModel> Success(AuthenticatedUserViewModel user) =>
        Result<RegistrationDataViewModel>.Success(new RegistrationDataViewModel { User = user });

    public static Result<RegistrationDataViewModel> UserAlreadyExists() =>
        Result<RegistrationDataViewModel>.Failure(CommonErrors.UserAlreadyExists);

    public static Result<RegistrationDataViewModel> ValidationFailed(string[] errors) =>
        Result<RegistrationDataViewModel>.Failure("VALIDATION_FAILED", string.Join(", ", errors));

    public static Result<RegistrationDataViewModel> Failure(string code, string message) =>
        Result<RegistrationDataViewModel>.Failure(new Error(code, message));
}