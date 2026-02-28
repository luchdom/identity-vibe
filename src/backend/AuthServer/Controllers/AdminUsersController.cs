using AuthServer.Models.AdminUsers;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Extensions;

namespace AuthServer.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminUsersManagementPolicy)]
public sealed class AdminUsersController(IUserAdminService userAdminService) : ControllerBase
{
    public const string AdminUsersManagementPolicy = "AdminUsersManagement";

    [HttpGet]
    public async Task<ActionResult<AdminUserListResponse>> ListUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? role,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await userAdminService.ListUsersAsync(search, isActive, role, pageNumber, pageSize);
        return ToActionResult(result);
    }

    [HttpGet("available-roles")]
    public async Task<ActionResult<IReadOnlyList<AdminRoleOptionResponse>>> GetAvailableRoles()
    {
        var result = await userAdminService.GetAvailableRolesAsync();
        return ToActionResult(result);
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<AdminUserDetailResponse>> GetUser(string userId)
    {
        var result = await userAdminService.GetUserAsync(userId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserDetailResponse>> CreateUser([FromBody] CreateUserRequest request)
    {
        var result = await userAdminService.CreateUserAsync(request);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetUser), new { userId = result.Value.Id }, result.Value);
        }

        return ToProblem(result.Error);
    }

    [HttpPut("{userId}")]
    public async Task<ActionResult<AdminUserDetailResponse>> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        var result = await userAdminService.UpdateUserAsync(userId, request);
        return ToActionResult(result);
    }

    [HttpPut("{userId}/roles")]
    public async Task<ActionResult<AdminUserDetailResponse>> UpdateUserRoles(string userId, [FromBody] UpdateUserRolesRequest request)
    {
        var actorUserId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return ToProblem(CommonErrors.Unauthorized);
        }

        var result = await userAdminService.UpdateUserRolesAsync(userId, request, actorUserId);
        return ToActionResult(result);
    }

    [HttpPut("{userId}/status")]
    public async Task<ActionResult<AdminUserDetailResponse>> UpdateUserStatus(string userId, [FromBody] UpdateUserStatusRequest request)
    {
        var actorUserId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return ToProblem(CommonErrors.Unauthorized);
        }

        var result = await userAdminService.UpdateUserStatusAsync(userId, request, actorUserId);
        return ToActionResult(result);
    }

    [HttpPut("{userId}/password")]
    public async Task<ActionResult<AdminUserDetailResponse>> ResetUserPassword(string userId, [FromBody] ResetUserPasswordRequest request)
    {
        var result = await userAdminService.ResetUserPasswordAsync(userId, request);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return ToProblem(result.Error);
    }

    private ActionResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "AUTH_UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
            "VALIDATION_FAILED" or "REQUIRED_FIELD" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}
