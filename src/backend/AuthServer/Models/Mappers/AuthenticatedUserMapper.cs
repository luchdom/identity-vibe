using AuthServer.Models.Results;
using AuthServer.Models.Responses;

namespace AuthServer.Models.Mappers;

public static class AuthenticatedUserMapper
{
    public static AuthenticatedUserResponse ToPresentation(this AuthenticatedUserResult viewModel)
    {
        return new AuthenticatedUserResponse
        {
            Id = viewModel.Id,
            Email = viewModel.Email,
            Roles = viewModel.Roles,
            IsActive = viewModel.IsActive,
            CreatedAt = viewModel.CreatedAt
        };
    }
}