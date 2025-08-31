using AuthServer.Entities;
using AuthServer.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Entities.Mappers;

public static class UserMapper
{
    public static AuthenticatedUser ToDomain(this User entity, IList<string>? roles = null)
    {
        return new AuthenticatedUser
        {
            Id = entity.Id,
            Email = entity.Email ?? string.Empty,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Roles = roles?.ToArray() ?? [],
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt
        };
    }

    public static User ToEntity(this AuthenticatedUser domain)
    {
        return new User
        {
            Id = domain.Id,
            Email = domain.Email,
            UserName = domain.Email,
            FirstName = domain.FirstName,
            LastName = domain.LastName,
            IsActive = domain.IsActive,
            CreatedAt = domain.CreatedAt,
            EmailConfirmed = true
        };
    }
}