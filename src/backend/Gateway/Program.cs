using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using Serilog;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);


// Configure automatic OpenTelemetry instrumentation
builder.AddOpenTelemetryAutoInstrumentation("gateway", "1.0.0");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add OpenIddict validation with local JWT validation
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Set the issuer (AuthServer URL)
        var authority = builder.Configuration["Authentication:Authority"]
                        ??  throw new ArgumentNullException("Authentication:Authority");
        Log.Information("Authorization settings - Authority: {Authority}", authority);
        options.SetIssuer(authority);

        options.Configure(options => options.TokenValidationParameters.ValidateAudience = false);

        // Register the ASP.NET Core host
        options.UseAspNetCore();

        // Configure caching for discovery document
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly);
    });


// Add authentication
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

// Add Authorization with simple policies
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .Build();

    // Anonymous policy - allows requests without authentication
    // options.AddPolicy("Anonymous", policy =>
    //     policy.RequireAssertion(_ => true));
});

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builder => builder.AddRequestTransform(async context =>
    {
        // Forward the authorization header if present
        if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            // var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            // context.ProxyRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
            //
            // var token = await context.HttpContext.GetTokenAsync("access_token");
            //
            // context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //
            // if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            // {
            //     context.ProxyRequest.Headers.Add("Authorization", authHeader.ToString());
            // }

            // if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            // {
            //     // Only set if not already present
            //     if (!context.ProxyRequest.Headers.Contains("Authorization"))
            //     {
            //         context.ProxyRequest.Headers.Authorization =
            //             new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            //                 authHeader.ToString().Replace("Bearer ", ""));
            //     }
            // }

            // if (context.HttpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
            // {
            //     var test = AuthenticationHeaderValue.Parse(authHeader);
            //
            //     context.ProxyRequest.Headers.Remove("Authorization");
            //
            //         // Get first header value and remove any whitespace/linebreaks
            //         var tokenValue = authHeader.FirstOrDefault()?.Replace("Bearer ", "")
            //             .Replace("\n", "")
            //             .Replace("\r", "")
            //             .Replace(" ", "")
            //             .Trim();
            //
            //         if (!string.IsNullOrEmpty(tokenValue))
            //         {
            //             context.ProxyRequest.Headers.Authorization =
            //                 new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenValue);
            //         }
            //
            // }
        }

        // Forward user claims as headers for downstream services
        if (context.HttpContext.User?.Identity?.IsAuthenticated == true)
        {
            var user = context.HttpContext.User;

            // Forward user ID
            var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
                context.ProxyRequest.Headers.Add("X-User-Id", userId);

            // Forward user email
            var email = user.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(email))
                context.ProxyRequest.Headers.Add("X-User-Email", email);

            // Forward roles as comma-separated list
            var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value);
            if (roles.Any())
                context.ProxyRequest.Headers.Add("X-User-Roles", string.Join(",", roles));
        }
    }));

// Add services to the container
builder.Services.AddControllers();

// Configure Problem Details
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = (context) =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;

        // Add traceId if available
        if (context.HttpContext.Request.Headers.ContainsKey("X-Correlation-ID"))
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.Request.Headers["X-Correlation-ID"].ToString();
        }
        else if (!string.IsNullOrEmpty(context.HttpContext.TraceIdentifier))
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        }
    };
});
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Add logging services
builder.Services.AddLoggingServices();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// Add OpenTelemetry middleware
app.UseOpenTelemetryMiddleware();

// Add logging middleware
app.UseLoggingMiddleware();
app.UseSerilogMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map YARP routes
app.MapReverseProxy();

// Health check endpoint (anonymous - no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();
