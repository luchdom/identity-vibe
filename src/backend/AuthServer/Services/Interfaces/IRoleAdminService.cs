using AuthServer.Models.AdminRoles;
using Shared.Common;

namespace AuthServer.Services.Interfaces;

public interface IRoleAdminService
{
    Task<Result<IReadOnlyList<AdminRoleResponse>>> ListRolesAsync();
    Task<Result<AdminRoleDetailResponse>> GetRoleAsync(string roleId);
    Result<IReadOnlyList<AvailableScopeResponse>> GetAvailableScopes();
    Task<Result<AdminRoleDetailResponse>> CreateRoleAsync(CreateRoleRequest request);
    Task<Result<AdminRoleDetailResponse>> UpdateRoleAsync(string roleId, UpdateRoleRequest request);
    Task<Result<AdminRoleDetailResponse>> UpdateRoleScopesAsync(string roleId, UpdateRoleScopesRequest request);
}
