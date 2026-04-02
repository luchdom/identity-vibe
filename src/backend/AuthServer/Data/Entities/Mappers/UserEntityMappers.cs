using AuthServer.Data.Entities;
using AuthServer.Models.Results;

namespace AuthServer.Data.Entities.Mappers;

public static class UserEntityMappers
{
    public static UserResult ToDomain(this AppUser entity) => new()
    {
        Id = entity.Id,
        Email = entity.Email ?? string.Empty,
        UserName = entity.UserName,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    public static AppUser ToEntity(this UserResult viewModel) => new()
    {
        Id = viewModel.Id,
        Email = viewModel.Email,
        NormalizedEmail = viewModel.Email.ToUpperInvariant(),
        UserName = viewModel.UserName ?? viewModel.Email,
        NormalizedUserName = (viewModel.UserName ?? viewModel.Email).ToUpperInvariant(),
        CreatedAt = viewModel.CreatedAt,
        IsActive = viewModel.IsActive
    };

    public static void UpdateEntity(this AppUser entity, UserResult viewModel)
    {
        entity.Email = viewModel.Email;
        entity.NormalizedEmail = viewModel.Email.ToUpperInvariant();
        entity.UserName = viewModel.UserName ?? viewModel.Email;
        entity.NormalizedUserName = (viewModel.UserName ?? viewModel.Email).ToUpperInvariant();
        entity.UpdatedAt = DateTime.UtcNow;
        entity.IsActive = viewModel.IsActive;
    }
}