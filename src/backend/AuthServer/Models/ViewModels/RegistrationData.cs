using AuthServer.Models.ViewModels;

namespace AuthServer.Models.ViewModels;

/// <summary>
/// Domain-specific registration result data
/// </summary>
public record RegistrationData
{
    public required AuthenticatedUser User { get; init; }
}