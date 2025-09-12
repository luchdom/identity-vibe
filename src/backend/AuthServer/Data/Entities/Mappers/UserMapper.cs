using AuthServer.Data.Entities;
using AuthServer.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace AuthServer.Data.Entities.Mappers;

public static class UserMapper
{
    public static AuthenticatedUserViewModel ToDomain(this AppUser entity, IList<string>? roles = null)
    {
        return new AuthenticatedUserViewModel
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

    public static AppUser ToEntity(this AuthenticatedUserViewModel domain)
    {
        return new AppUser
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