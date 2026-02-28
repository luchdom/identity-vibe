using System.Text.RegularExpressions;
using AuthServer.Data;
using AuthServer.Data.Entities;
using AuthServer.Models.AdminRoles;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Common;

namespace AuthServer.Services;

public sealed class RoleAdminService(
    AppDbContext dbContext,
    RoleManager<AppRole> roleManager,
    ScopeConfigurationService scopeConfigurationService,
    ILogger<RoleAdminService> logger) : IRoleAdminService
{
    private static readonly Regex RoleNameRegex = new("^[A-Za-z0-9._-]{2,64}$", RegexOptions.Compiled);

    public async Task<Result<IReadOnlyList<AdminRoleResponse>>> ListRolesAsync()
    {
        try
        {
            var roles = await dbContext.Roles
                .AsNoTracking()
                .Include(r => r.RoleScopes)
                .OrderBy(r => r.Name)
                .Select(r => new AdminRoleResponse
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    AssignedScopes = r.RoleScopes
                        .Select(rs => rs.ScopeName)
                        .OrderBy(x => x)
                        .ToArray(),
                    AssignedScopeCount = r.RoleScopes.Count
                })
                .ToListAsync();

            return Result<IReadOnlyList<AdminRoleResponse>>.Success(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list roles");
            return Result<IReadOnlyList<AdminRoleResponse>>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminRoleDetailResponse>> GetRoleAsync(string roleId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.RequiredField("roleId"));
        }

        try
        {
            var role = await dbContext.Roles
                .AsNoTracking()
                .Include(r => r.RoleScopes)
                .SingleOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return Result<AdminRoleDetailResponse>.Failure(CommonErrors.NotFound("Role"));
            }

            return Result<AdminRoleDetailResponse>.Success(MapDetail(role));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get role {RoleId}", roleId);
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public Result<IReadOnlyList<AvailableScopeResponse>> GetAvailableScopes()
    {
        var scopes = scopeConfigurationService.GetAssignableRoleScopes()
            .OrderBy(s => s.Name)
            .Select(s => new AvailableScopeResponse
            {
                Name = s.Name,
                DisplayName = s.DisplayName,
                Description = s.Description,
                Resources = s.Resources,
                Group = InferScopeGroup(s.Name)
            })
            .ToList();

        return Result<IReadOnlyList<AvailableScopeResponse>>.Success(scopes);
    }

    public async Task<Result<AdminRoleDetailResponse>> CreateRoleAsync(CreateRoleRequest request)
    {
        var validation = ValidateCreateRequest(request);
        if (validation.IsFailure)
        {
            return Result<AdminRoleDetailResponse>.Failure(validation.Error);
        }

        try
        {
            if (await roleManager.RoleExistsAsync(request.Name))
            {
                return Result<AdminRoleDetailResponse>.Failure(CommonErrors.Conflict($"Role '{request.Name}' already exists"));
            }

            var role = new AppRole
            {
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                IsActive = request.IsActive
            };

            var result = await roleManager.CreateAsync(role);
            if (!result.Succeeded)
            {
                return Result<AdminRoleDetailResponse>.Failure(CommonErrors.ValidationFailed(string.Join("; ", result.Errors.Select(e => e.Description))));
            }

            var created = await dbContext.Roles
                .AsNoTracking()
                .Include(r => r.RoleScopes)
                .SingleAsync(r => r.Id == role.Id);

            return Result<AdminRoleDetailResponse>.Success(MapDetail(created));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create role {RoleName}", request.Name);
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminRoleDetailResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.RequiredField("roleId"));
        }

        if (request.Description?.Length > 500)
        {
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.ValidationFailed("Description cannot exceed 500 characters."));
        }

        try
        {
            var role = await dbContext.Roles
                .Include(r => r.RoleScopes)
                .SingleOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return Result<AdminRoleDetailResponse>.Failure(CommonErrors.NotFound("Role"));
            }

            role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();

            return Result<AdminRoleDetailResponse>.Success(MapDetail(role));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update role metadata for {RoleId}", roleId);
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminRoleDetailResponse>> UpdateRoleScopesAsync(string roleId, UpdateRoleScopesRequest request)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.RequiredField("roleId"));
        }

        var normalizedScopes = (request?.ScopeNames ?? [])
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var invalidScopes = normalizedScopes
            .Where(scope => !scopeConfigurationService.IsAssignableRoleScope(scope))
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            return Result<AdminRoleDetailResponse>.Failure(
                CommonErrors.ValidationFailed($"Invalid assignable scope(s): {string.Join(", ", invalidScopes)}"));
        }

        try
        {
            var role = await dbContext.Roles
                .Include(r => r.RoleScopes)
                .SingleOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return Result<AdminRoleDetailResponse>.Failure(CommonErrors.NotFound("Role"));
            }

            var existing = dbContext.RoleScopes.Where(rs => rs.RoleId == roleId);
            dbContext.RoleScopes.RemoveRange(existing);

            if (normalizedScopes.Length > 0)
            {
                var newRows = normalizedScopes.Select(scope => new AppRoleScope
                {
                    RoleId = roleId,
                    ScopeName = scope
                });

                await dbContext.RoleScopes.AddRangeAsync(newRows);
            }

            role.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();

            var updatedRole = await dbContext.Roles
                .AsNoTracking()
                .Include(r => r.RoleScopes)
                .SingleAsync(r => r.Id == roleId);

            return Result<AdminRoleDetailResponse>.Success(MapDetail(updatedRole));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update role scopes for {RoleId}", roleId);
            return Result<AdminRoleDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    private static Result ValidateCreateRequest(CreateRoleRequest? request)
    {
        if (request is null)
        {
            return Result.Failure(CommonErrors.RequiredField("request"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure(CommonErrors.RequiredField("name"));
        }

        var roleName = request.Name.Trim();
        if (!RoleNameRegex.IsMatch(roleName))
        {
            return Result.Failure(CommonErrors.ValidationFailed("Role name must be 2-64 chars and use letters, numbers, dot, dash, or underscore."));
        }

        if (request.Description?.Length > 500)
        {
            return Result.Failure(CommonErrors.ValidationFailed("Description cannot exceed 500 characters."));
        }

        return Result.Success();
    }

    private static string InferScopeGroup(string scopeName)
    {
        var index = scopeName.IndexOf('.');
        return index <= 0 ? "other" : scopeName[..index];
    }

    private static AdminRoleDetailResponse MapDetail(AppRole role)
    {
        return new AdminRoleDetailResponse
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            IsActive = role.IsActive,
            AssignedScopes = role.RoleScopes.Select(rs => rs.ScopeName).OrderBy(x => x).ToArray(),
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
    }
}
