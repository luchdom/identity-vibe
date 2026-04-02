using AuthServer.Models.AdminRoles;
using AuthServer.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace AuthServer.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = AdminRolesManagementPolicy)]
public sealed class AdminRolesController(
    IRoleAdminService roleAdminService,
    IValidator<CreateRoleRequest> createRoleRequestValidator,
    IValidator<UpdateRoleRequest> updateRoleRequestValidator,
    IValidator<UpdateRoleScopesRequest> updateRoleScopesRequestValidator) : ControllerBase
{
    public const string AdminRolesManagementPolicy = "AdminRolesManagement";

    [HttpGet]
    public async Task<IActionResult> ListRoles()
    {
        var result = await roleAdminService.ListRolesAsync();
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpGet("available-scopes")]
    public IActionResult GetAvailableScopes()
    {
        var result = roleAdminService.GetAvailableScopes();
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetRole(string roleId)
    {
        var routeGuard = RequireRouteValue<AdminRoleDetailResponse>(roleId, "roleId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var result = await roleAdminService.GetRoleAsync(roleId);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var validationResult = await createRoleRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await roleAdminService.CreateRoleAsync(validationResult.Value);
        return result.ToActionResultWithProblemDetails(
            HttpContext,
            value => CreatedAtAction(nameof(GetRole), new { roleId = value.Id }, value));
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request)
    {
        var routeGuard = RequireRouteValue<AdminRoleDetailResponse>(roleId, "roleId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var validationResult = await updateRoleRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await roleAdminService.UpdateRoleAsync(roleId, validationResult.Value);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    [HttpPut("{roleId}/scopes")]
    public async Task<IActionResult> UpdateRoleScopes(string roleId, [FromBody] UpdateRoleScopesRequest request)
    {
        var routeGuard = RequireRouteValue<AdminRoleDetailResponse>(roleId, "roleId");
        if (routeGuard is not null)
        {
            return routeGuard;
        }

        var validationResult = await updateRoleScopesRequestValidator.ValidateRequestAsync(request, HttpContext.RequestAborted);
        if (!validationResult.IsSuccess)
        {
            return validationResult.ToActionResultWithProblemDetails(HttpContext);
        }

        var result = await roleAdminService.UpdateRoleScopesAsync(roleId, validationResult.Value);
        return result.ToActionResultWithProblemDetails(HttpContext, value => Ok(value));
    }

    private IActionResult? RequireRouteValue<TResponse>(string? value, string fieldName)
    {
        return string.IsNullOrWhiteSpace(value)
            ? CommonErrors.RequiredField(fieldName).ToResult<TResponse>().ToActionResultWithProblemDetails(HttpContext)
            : null;
    }
}
