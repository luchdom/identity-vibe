using AuthServer.Data.Entities;
using AuthServer.Models.ViewModels;

namespace AuthServer.Data.Entities.Mappers;

public static class UserEntityMappers
{
    public static UserViewModel ToDomain(this AppUser entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email ?? string.Empty,
        UserName = entity.UserName ?? string.Empty,
        FirstName = entity.FirstName,
        LastName = entity.LastName,
        EmailConfirmed = entity.EmailConfirmed,
        PhoneNumber = entity.PhoneNumber,
        PhoneNumberConfirmed = entity.PhoneNumberConfirmed,
        TwoFactorEnabled = entity.TwoFactorEnabled,
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        IsActive = entity.IsActive,
        LockoutEnd = entity.LockoutEnd,
        LockoutEnabled = entity.LockoutEnabled,
        AccessFailedCount = entity.AccessFailedCount
    };

    public static AppUser ToEntity(this UserViewModel viewModel) => new()
    {
        Id = viewModel.Id,
        Email = viewModel.Email,
        NormalizedEmail = viewModel.Email.ToUpperInvariant(),
        UserName = viewModel.UserName,
        NormalizedUserName = viewModel.UserName.ToUpperInvariant(),
        FirstName = viewModel.FirstName,
        LastName = viewModel.LastName,
        EmailConfirmed = viewModel.EmailConfirmed,
        PhoneNumber = viewModel.PhoneNumber,
        PhoneNumberConfirmed = viewModel.PhoneNumberConfirmed,
        TwoFactorEnabled = viewModel.TwoFactorEnabled,
        CreatedAt = viewModel.CreatedAt,
        UpdatedAt = viewModel.UpdatedAt,
        IsActive = viewModel.IsActive,
        LockoutEnd = viewModel.LockoutEnd,
        LockoutEnabled = viewModel.LockoutEnabled,
        AccessFailedCount = viewModel.AccessFailedCount
    };

    public static void UpdateEntity(this AppUser entity, UserViewModel viewModel)
    {
        entity.Email = viewModel.Email;
        entity.NormalizedEmail = viewModel.Email.ToUpperInvariant();
        entity.UserName = viewModel.UserName;
        entity.NormalizedUserName = viewModel.UserName.ToUpperInvariant();
        entity.FirstName = viewModel.FirstName;
        entity.LastName = viewModel.LastName;
        entity.EmailConfirmed = viewModel.EmailConfirmed;
        entity.PhoneNumber = viewModel.PhoneNumber;
        entity.PhoneNumberConfirmed = viewModel.PhoneNumberConfirmed;
        entity.TwoFactorEnabled = viewModel.TwoFactorEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = viewModel.IsActive;
        entity.LockoutEnd = viewModel.LockoutEnd;
        entity.LockoutEnabled = viewModel.LockoutEnabled;
        entity.AccessFailedCount = viewModel.AccessFailedCount;
    }
}