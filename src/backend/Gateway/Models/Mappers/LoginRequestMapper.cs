using Gateway.Models.Requests;

namespace Gateway.Models.Mappers;

public static class LoginRequestMapper
{
    public static LoginRequest ToDomain(this LoginRequest request)
    {
        return request; // Gateway just passes through to AuthServer
    }
}