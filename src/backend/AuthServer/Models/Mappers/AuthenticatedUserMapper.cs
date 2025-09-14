using AuthServer.Models.ViewModels;
using AuthServer.Models.Responses;

namespace AuthServer.Models.Mappers;

public static class AuthenticatedUserMapper
{
    public static AuthenticatedUserResponse ToPresentation(this AuthenticatedUserViewModel viewModel)
    {
        return new AuthenticatedUserResponse
        {
            Id = viewModel.Id,
            Email = viewModel.Email,
            FirstName = viewModel.FirstName,
            LastName = viewModel.LastName,
            Roles = viewModel.Roles,
            IsActive = viewModel.IsActive,
            CreatedAt = viewModel.CreatedAt
        };
    }
}