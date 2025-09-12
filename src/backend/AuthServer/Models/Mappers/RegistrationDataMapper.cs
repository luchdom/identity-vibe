using AuthServer.Models.ViewModels;
using AuthServer.Models.Responses;

namespace AuthServer.Models.Mappers;

public static class RegistrationDataMapper
{
    public static RegistrationResponse ToPresentation(this RegistrationDataViewModel data)
    {
        return new RegistrationResponse
        {
            Success = true,
            Message = "Registration successful",
            User = data.User
        };
    }
}