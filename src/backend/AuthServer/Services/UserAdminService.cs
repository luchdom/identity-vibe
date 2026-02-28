using AuthServer.Data;
using AuthServer.Data.Entities;
using AuthServer.Models.AdminUsers;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Common;

namespace AuthServer.Services;

public sealed class UserAdminService(
    AppDbContext dbContext,
    UserManager<AppUser> userManager,
    ILogger<UserAdminService> logger) : IUserAdminService
{
    private static readonly StringComparer RoleNameComparer = StringComparer.OrdinalIgnoreCase;

    public async Task<Result<AdminUserListResponse>> ListUsersAsync(
        string? search,
        bool? isActive,
        string? role,
        int pageNumber,
        int pageSize)
    {
        var safePageNumber = pageNumber <= 0 ? 1 : pageNumber;
        var safePageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 100);

        try
        {
            var query = dbContext.Users.AsNoTracking().AsQueryable();

            if (isActive.HasValue)
            {
                query = query.Where(user => user.IsActive == isActive.Value);
            }

            var trimmedSearch = search?.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedSearch))
            {
                var pattern = $"%{trimmedSearch}%";
                query = query.Where(user =>
                    (user.Email != null && EF.Functions.ILike(user.Email, pattern)) ||
                    (user.UserName != null && EF.Functions.ILike(user.UserName, pattern)) ||
                    (user.PhoneNumber != null && EF.Functions.ILike(user.PhoneNumber, pattern)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var normalizedRole = role.Trim().ToUpperInvariant();
                var roleId = await dbContext.Roles
                    .AsNoTracking()
                    .Where(r => r.NormalizedName == normalizedRole)
                    .Select(r => r.Id)
                    .SingleOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(roleId))
                {
                    return Result<AdminUserListResponse>.Success(new AdminUserListResponse
                    {
                        Users = [],
                        TotalCount = 0,
                        PageNumber = safePageNumber,
                        PageSize = safePageSize,
                        TotalPages = 0,
                        HasPreviousPage = false,
                        HasNextPage = false
                    });
                }

                query = query.Where(user =>
                    dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleId));
            }

            var totalCount = await query.CountAsync();

            var usersPage = await query
                .OrderBy(user => user.Email ?? user.UserName ?? user.Id)
                .Skip((safePageNumber - 1) * safePageSize)
                .Take(safePageSize)
                .Select(user => new
                {
                    user.Id,
                    user.Email,
                    user.UserName,
                    user.PhoneNumber,
                    user.IsActive,
                    user.CreatedAt,
                    user.UpdatedAt
                })
                .ToListAsync();

            var roleLookup = await GetRolesByUserIdAsync(usersPage.Select(user => user.Id).ToArray());

            var summaries = usersPage
                .Select(user => new AdminUserSummaryResponse
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    Roles = roleLookup.TryGetValue(user.Id, out var names) ? names : [],
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                })
                .ToArray();

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)safePageSize);

            return Result<AdminUserListResponse>.Success(new AdminUserListResponse
            {
                Users = summaries,
                TotalCount = totalCount,
                PageNumber = safePageNumber,
                PageSize = safePageSize,
                TotalPages = totalPages,
                HasPreviousPage = safePageNumber > 1 && totalPages > 0,
                HasNextPage = safePageNumber < totalPages
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list admin users");
            return Result<AdminUserListResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> GetUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("userId"));
        }

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.NotFound("User"));
            }

            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<IReadOnlyList<AdminRoleOptionResponse>>> GetAvailableRolesAsync()
    {
        try
        {
            var roles = await dbContext.Roles
                .AsNoTracking()
                .Include(r => r.RoleScopes)
                .OrderBy(r => r.Name)
                .Select(r => new AdminRoleOptionResponse
                {
                    Id = r.Id,
                    Name = r.Name ?? string.Empty,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    AssignedScopes = r.RoleScopes
                        .Select(scope => scope.ScopeName)
                        .OrderBy(scope => scope)
                        .ToArray()
                })
                .ToListAsync();

            return Result<IReadOnlyList<AdminRoleOptionResponse>>.Success(roles);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list available roles for user administration");
            return Result<IReadOnlyList<AdminRoleOptionResponse>>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> CreateUserAsync(CreateUserRequest request)
    {
        if (request is null)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("request"));
        }

        var normalizedEmail = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("email"));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("password"));
        }

        var roleNames = NormalizeRoleNames(request.RoleNames);
        if (roleNames.Length == 0)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed("At least one active role is required."));
        }

        try
        {
            var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser != null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.Conflict($"User '{normalizedEmail}' already exists."));
            }

            var resolvedRolesResult = await ResolveActiveRolesAsync(roleNames);
            if (resolvedRolesResult.IsFailure)
            {
                return Result<AdminUserDetailResponse>.Failure(resolvedRolesResult.Error);
            }

            var user = new AppUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                EmailConfirmed = true,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed(FormatIdentityErrors(createResult.Errors)));
            }

            var roleAssignmentResult = await userManager.AddToRolesAsync(user, roleNames);
            if (!roleAssignmentResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed(FormatIdentityErrors(roleAssignmentResult.Errors)));
            }

            logger.LogInformation("Admin user created. UserId={UserId}, Email={Email}", user.Id, user.Email);
            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create admin user for email {Email}", normalizedEmail);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("userId"));
        }

        if (request is null)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("request"));
        }

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.NotFound("User"));
            }

            var hasChanges = false;
            var nextEmail = request.Email?.Trim();
            if (nextEmail != null)
            {
                if (string.IsNullOrWhiteSpace(nextEmail))
                {
                    return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed("Email cannot be empty."));
                }

                if (!string.Equals(user.Email, nextEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var existingByEmail = await userManager.FindByEmailAsync(nextEmail);
                    if (existingByEmail != null && !string.Equals(existingByEmail.Id, user.Id, StringComparison.Ordinal))
                    {
                        return Result<AdminUserDetailResponse>.Failure(CommonErrors.Conflict($"Email '{nextEmail}' is already in use."));
                    }

                    user.Email = nextEmail;
                    user.UserName = nextEmail;
                    hasChanges = true;
                }
            }

            var normalizedPhone = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            if (!string.Equals(user.PhoneNumber, normalizedPhone, StringComparison.Ordinal))
            {
                user.PhoneNumber = normalizedPhone;
                hasChanges = true;
            }

            if (hasChanges)
            {
                user.UpdatedAt = DateTime.UtcNow;
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed(FormatIdentityErrors(updateResult.Errors)));
                }
            }

            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> UpdateUserRolesAsync(
        string userId,
        UpdateUserRolesRequest request,
        string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("userId"));
        }

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.Unauthorized);
        }

        if (request is null)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("request"));
        }

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.NotFound("User"));
            }

            var normalizedRoleNames = NormalizeRoleNames(request.RoleNames);
            var resolvedRolesResult = await ResolveActiveRolesAsync(normalizedRoleNames);
            if (resolvedRolesResult.IsFailure)
            {
                return Result<AdminUserDetailResponse>.Failure(resolvedRolesResult.Error);
            }

            var resolvedRoles = resolvedRolesResult.Value;
            if (string.Equals(userId, actorUserId, StringComparison.Ordinal))
            {
                var hasAdminManagementScope = await HasAdminManagementScopeAsync(resolvedRoles.Select(role => role.Id).ToArray());
                if (!hasAdminManagementScope)
                {
                    return Result<AdminUserDetailResponse>.Failure(
                        CommonErrors.ValidationFailed("Cannot remove your own admin.users/admin.full access."));
                }
            }

            var existingRows = dbContext.UserRoles.Where(userRole => userRole.UserId == userId);
            dbContext.UserRoles.RemoveRange(existingRows);

            if (resolvedRoles.Count > 0)
            {
                var newRows = resolvedRoles.Select(role => new AppUserRole
                {
                    UserId = userId,
                    RoleId = role.Id,
                    AssignedAt = DateTime.UtcNow,
                    AssignedBy = actorUserId
                });

                await dbContext.UserRoles.AddRangeAsync(newRows);
            }

            user.UpdatedAt = DateTime.UtcNow;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();

            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update roles for user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> UpdateUserStatusAsync(
        string userId,
        UpdateUserStatusRequest request,
        string actorUserId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("userId"));
        }

        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.Unauthorized);
        }

        if (request is null)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("request"));
        }

        if (string.Equals(userId, actorUserId, StringComparison.Ordinal) && !request.IsActive)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed("You cannot disable your own account."));
        }

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.NotFound("User"));
            }

            if (user.IsActive != request.IsActive)
            {
                user.IsActive = request.IsActive;
                user.UpdatedAt = DateTime.UtcNow;
                dbContext.Users.Update(user);
                await dbContext.SaveChangesAsync();
            }

            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    public async Task<Result<AdminUserDetailResponse>> ResetUserPasswordAsync(string userId, ResetUserPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("userId"));
        }

        if (request is null)
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("request"));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.RequiredField("newPassword"));
        }

        try
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.NotFound("User"));
            }

            var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);

            if (!resetResult.Succeeded)
            {
                return Result<AdminUserDetailResponse>.Failure(CommonErrors.ValidationFailed(FormatIdentityErrors(resetResult.Errors)));
            }

            user.UpdatedAt = DateTime.UtcNow;
            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Password reset completed for user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Success(await MapUserDetailAsync(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset password for user {UserId}", userId);
            return Result<AdminUserDetailResponse>.Failure(CommonErrors.DatabaseError);
        }
    }

    private async Task<AdminUserDetailResponse> MapUserDetailAsync(AppUser user)
    {
        var roleNames = (await userManager.GetRolesAsync(user))
            .OrderBy(role => role, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AdminUserDetailResponse
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName ?? user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roleNames,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private async Task<Dictionary<string, string[]>> GetRolesByUserIdAsync(string[] userIds)
    {
        if (userIds.Length == 0)
        {
            return new Dictionary<string, string[]>(StringComparer.Ordinal);
        }

        var roleRows = await dbContext.UserRoles
            .AsNoTracking()
            .Where(userRole => userIds.Contains(userRole.UserId))
            .Join(
                dbContext.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                role => role.Id,
                (userRole, role) => new { userRole.UserId, role.Name })
            .Where(row => row.Name != null)
            .ToListAsync();

        return roleRows
            .GroupBy(row => row.UserId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => row.Name!)
                    .Distinct(RoleNameComparer)
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private async Task<Result<List<AppRole>>> ResolveActiveRolesAsync(string[] normalizedRoleNames)
    {
        if (normalizedRoleNames.Length == 0)
        {
            return Result<List<AppRole>>.Success([]);
        }

        var normalizedRoleNameSet = normalizedRoleNames
            .Select(name => name.ToUpperInvariant())
            .ToArray();

        var roles = await dbContext.Roles
            .Where(role => role.NormalizedName != null && normalizedRoleNameSet.Contains(role.NormalizedName))
            .ToListAsync();

        var foundRoleNames = roles
            .Select(role => role.Name ?? string.Empty)
            .ToHashSet(RoleNameComparer);

        var missingRoleNames = normalizedRoleNames
            .Where(roleName => !foundRoleNames.Contains(roleName))
            .OrderBy(roleName => roleName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (missingRoleNames.Length > 0)
        {
            return Result<List<AppRole>>.Failure(
                CommonErrors.ValidationFailed($"Unknown role(s): {string.Join(", ", missingRoleNames)}"));
        }

        var inactiveRoleNames = roles
            .Where(role => !role.IsActive)
            .Select(role => role.Name ?? string.Empty)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (inactiveRoleNames.Length > 0)
        {
            return Result<List<AppRole>>.Failure(
                CommonErrors.ValidationFailed($"Inactive role(s) cannot be assigned: {string.Join(", ", inactiveRoleNames)}"));
        }

        return Result<List<AppRole>>.Success(roles);
    }

    private async Task<bool> HasAdminManagementScopeAsync(string[] roleIds)
    {
        if (roleIds.Length == 0)
        {
            return false;
        }

        return await dbContext.RoleScopes
            .AsNoTracking()
            .AnyAsync(scope =>
                roleIds.Contains(scope.RoleId) &&
                (scope.ScopeName == "admin.users" || scope.ScopeName == "admin.full"));
    }

    private static string[] NormalizeRoleNames(string[]? roleNames)
    {
        return (roleNames ?? [])
            .Where(roleName => !string.IsNullOrWhiteSpace(roleName))
            .Select(roleName => roleName.Trim())
            .Distinct(RoleNameComparer)
            .ToArray();
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
    {
        return string.Join("; ", errors.Select(error => error.Description));
    }
}
