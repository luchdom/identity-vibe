using AuthServer.Models.AdminRoles;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common;

namespace AuthServer.Controllers;

[ApiController]
[Route("api/admin/roles")]
[Authorize(Policy = AdminRolesManagementPolicy)]
public sealed class AdminRolesController(IRoleAdminService roleAdminService) : ControllerBase
{
    public const string AdminRolesManagementPolicy = "AdminRolesManagement";

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminRoleResponse>>> ListRoles()
    {
        var result = await roleAdminService.ListRolesAsync();
        return ToActionResult(result);
    }

    [HttpGet("available-scopes")]
    public ActionResult<IReadOnlyList<AvailableScopeResponse>> GetAvailableScopes()
    {
        var result = roleAdminService.GetAvailableScopes();
        return ToActionResult(result);
    }

    [HttpGet("{roleId}")]
    public async Task<ActionResult<AdminRoleDetailResponse>> GetRole(string roleId)
    {
        var result = await roleAdminService.GetRoleAsync(roleId);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminRoleDetailResponse>> CreateRole([FromBody] CreateRoleRequest request)
    {
        var result = await roleAdminService.CreateRoleAsync(request);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetRole), new { roleId = result.Value.Id }, result.Value);
        }

        return ToProblem(result.Error);
    }

    [HttpPut("{roleId}")]
    public async Task<ActionResult<AdminRoleDetailResponse>> UpdateRole(string roleId, [FromBody] UpdateRoleRequest request)
    {
        var result = await roleAdminService.UpdateRoleAsync(roleId, request);
        return ToActionResult(result);
    }

    [HttpPut("{roleId}/scopes")]
    public async Task<ActionResult<AdminRoleDetailResponse>> UpdateRoleScopes(string roleId, [FromBody] UpdateRoleScopesRequest request)
    {
        var result = await roleAdminService.UpdateRoleScopesAsync(roleId, request);
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
            "VALIDATION_FAILED" or "REQUIRED_FIELD" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            statusCode: statusCode,
            title: error.Code,
            detail: error.Message);
    }
}
