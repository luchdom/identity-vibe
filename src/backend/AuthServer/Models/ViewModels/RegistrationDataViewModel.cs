using AuthServer.Models.ViewModels;

namespace AuthServer.Models.ViewModels;

/// <summary>
/// Domain-specific registration result data
/// </summary>
public record RegistrationDataViewModel
{
    public required AuthenticatedUserViewModel User { get; init; }
}