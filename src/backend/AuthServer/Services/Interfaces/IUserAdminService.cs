using AuthServer.Models.AdminUsers;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IUserAdminService
{
    Task<Result<AdminUserListResponse>> ListUsersAsync(
        string? search,
        bool? isActive,
        string? role,
        int pageNumber,
        int pageSize);

    Task<Result<AdminUserDetailResponse>> GetUserAsync(string userId);

    Task<Result<IReadOnlyList<AdminRoleOptionResponse>>> GetAvailableRolesAsync();

    Task<Result<AdminUserDetailResponse>> CreateUserAsync(CreateUserRequest request);

    Task<Result<AdminUserDetailResponse>> UpdateUserAsync(string userId, UpdateUserRequest request);

    Task<Result<AdminUserDetailResponse>> UpdateUserRolesAsync(
        string userId,
        UpdateUserRolesRequest request,
        string actorUserId);

    Task<Result<AdminUserDetailResponse>> UpdateUserStatusAsync(
        string userId,
        UpdateUserStatusRequest request,
        string actorUserId);

    Task<Result<AdminUserDetailResponse>> ResetUserPasswordAsync(string userId, ResetUserPasswordRequest request);
}
