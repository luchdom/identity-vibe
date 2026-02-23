using AuthServer.Data.Entities;
using AuthServer.Models.ViewModels;

namespace AuthServer.Data.Entities.Mappers;

public static class UserEntityMappers
{
    public static UserViewModel ToDomain(this AppUser entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email ?? string.Empty,
        UserName = entity.UserName,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static AppUser ToEntity(this UserViewModel viewModel) => new()
    {
        Id = viewModel.Id,
        Email = viewModel.Email,
        NormalizedEmail = viewModel.Email.ToUpperInvariant(),
        UserName = viewModel.UserName ?? viewModel.Email,
        NormalizedUserName = (viewModel.UserName ?? viewModel.Email).ToUpperInvariant(),
        CreatedAt = viewModel.CreatedAt,
        IsActive = viewModel.IsActive
    };

    public static void UpdateEntity(this AppUser entity, UserViewModel viewModel)
    {
        entity.Email = viewModel.Email;
        entity.NormalizedEmail = viewModel.Email.ToUpperInvariant();
        entity.UserName = viewModel.UserName ?? viewModel.Email;
        entity.NormalizedUserName = (viewModel.UserName ?? viewModel.Email).ToUpperInvariant();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = viewModel.IsActive;
    }
}