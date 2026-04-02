using AuthServer.Data.Entities;
using AuthServer.Models.Results;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Entities.Mappers;

public static class UserMapper
{
    public static AuthenticatedUserResult ToDomain(this AppUser entity, IList<string>? roles = null)
    {
        return new AuthenticatedUserResult
        {
            Id = entity.Id,
            Email = entity.Email ?? string.Empty,
            Roles = roles?.ToArray() ?? [],
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public static AppUser ToEntity(this AuthenticatedUserResult domain)
    {
        return new AppUser
        {
            Id = domain.Id,
            Email = domain.Email,
            UserName = domain.Email,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            EmailConfirmed = true
        };
    }
}
