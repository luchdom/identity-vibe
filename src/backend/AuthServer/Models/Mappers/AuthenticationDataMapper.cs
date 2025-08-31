using AuthServer.Models.ViewModels;
using AuthServer.Models.Responses;

namespace AuthServer.Models.Mappers;

public static class AuthenticationDataMapper
{
    public static AuthenticationResponse ToPresentation(this AuthenticationData data)
    {
        return new AuthenticationResponse
        {
            Success = true,
            Message = "Authentication successful",
            User = data.User,
            AccessToken = data.AccessToken,
            GrantedScopes = data.GrantedScopes
        };
    }
}