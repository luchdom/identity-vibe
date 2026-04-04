using AuthServer.Models.AdminUsers;
using AuthServer.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;
using Shared.Extensions;

namespace AuthServer.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AdminUsersManagementPolicy)]
public sealed class AdminUsersController(
    IUserAdminService userAdminService,
    IValidator<CreateUserRequest> createUserRequestValidator,
    IValidator<UpdateUserRequest> updateUserRequestValidator,
    IValidator<UpdateUserRolesRequest> updateUserRolesRequestValidator,
    IValidator<UpdateUserStatusRequest> updateUserStatusRequestValidator,
    IValidator<ResetUserPasswordRequest> resetUserPasswordRequestValidator) : ControllerBase
{
    public const string AdminUsersManagementPolicy = "AdminUsersManagement";

    [HttpGet]
    public async Task<IActionResult> ListUsers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] string? role,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        (pageNumber, pageSize) = ValidationExtensions.ValidatePagination(pageNumber, pageSize);
        var result = await userAdminService.ListUsersAsync(search, isActive, role, pageNumber, pageSize);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpGet("available-roles")]
    public async Task<IActionResult> GetAvailableRoles()
    {
        var result = await userAdminService.GetAvailableRolesAsync();
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var routeGuard = RequireRouteValue<AdminUserDetailResponse>(userId, "userId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var result = await userAdminService.GetUserAsync(userId);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var validationResult = await createUserRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await userAdminService.CreateUserAsync(validationResult.Value);
        return result.ToActionResultWithProblemDetails(
            HttpContext,
            value => CreatedAtAction(nameof(GetUser), new { userId = value.Id }, value));
    }

    [HttpPut("{userId}")]
    public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserRequest request)
    {
        var routeGuard = RequireRouteValue<AdminUserDetailResponse>(userId, "userId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var validationResult = await updateUserRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await userAdminService.UpdateUserAsync(userId, validationResult.Value);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPut("{userId}/roles")]
    public async Task<IActionResult> UpdateUserRoles(string userId, [FromBody] UpdateUserRolesRequest request)
    {
        var routeGuard = RequireRouteValue<AdminUserDetailResponse>(userId, "userId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var actorUserId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return CommonErrors.Unauthorized.ToResult<AdminUserDetailResponse>()
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await updateUserRolesRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await userAdminService.UpdateUserRolesAsync(userId, validationResult.Value, actorUserId);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPut("{userId}/status")]
    public async Task<IActionResult> UpdateUserStatus(string userId, [FromBody] UpdateUserStatusRequest request)
    {
        var routeGuard = RequireRouteValue<AdminUserDetailResponse>(userId, "userId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var actorUserId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(actorUserId))
        {
            return CommonErrors.Unauthorized.ToResult<AdminUserDetailResponse>()
                .ToActionResultWithProblemDetails(HttpContext);
        }

        var validationResult = await updateUserStatusRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await userAdminService.UpdateUserStatusAsync(userId, validationResult.Value, actorUserId);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPut("{userId}/password")]
    public async Task<IActionResult> ResetUserPassword(string userId, [FromBody] ResetUserPasswordRequest request)
    {
        var routeGuard = RequireRouteValue<AdminUserDetailResponse>(userId, "userId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var validationResult = await resetUserPasswordRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await userAdminService.ResetUserPasswordAsync(userId, validationResult.Value);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    private IActionResult? RequireRouteValue<TResponse>(string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? CommonErrors.RequiredField(fieldName).ToResult<TResponse>().ToActionResultWithProblemDetails(HttpContext)
            : null;
    }
}
