using Microsoft.AspNetCore.Mvc;
using AuthServer.Models.Requests;
using AuthServer.Models.Mappers;
using AuthServer.Services.Interfaces;

namespace AuthServer.Controllers;

public class AccountController(IAuthenticationService authenticationService) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var request = new LoginRequest
        {
            ReturnUrl = returnUrl ?? Url.Content("~/")
        };
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        // Map Request → Command (API → Domain)
        var command = request.ToDomain();

        // Business logic in service layer
        var result = await authenticationService.LoginAsync(command);

        if (result.Success)
        {
            return LocalRedirect(request.ReturnUrl ?? Url.Content("~/"));
        }

        // Keep the form data and add error message
        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid login attempt.");
        return View(request);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        var request = new RegisterRequest
        {
            ReturnUrl = returnUrl ?? Url.Content("~/")
        };
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        // Map Request → Command (API → Domain)
        var command = request.ToDomain();

        // Business logic in service layer
        var result = await authenticationService.RegisterAsync(command);

        if (result.Success)
        {
            return LocalRedirect(request.ReturnUrl ?? Url.Content("~/"));
        }

        // Keep the form data and add error message
        request.ErrorMessage = result.ErrorMessage ?? "Registration failed.";
        return View(request);
    }
}