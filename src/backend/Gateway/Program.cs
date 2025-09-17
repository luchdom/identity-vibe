using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
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
        policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add OpenIddict validation with introspection
builder.Services.AddOpenIddict()
    .AddValidation(options =>
    {
        // Set the issuer (AuthServer URL)
        var authority = builder.Configuration["OpenIddict:Authority"];
        options.SetIssuer(authority);

        // Configure introspection for token validation
        options.UseIntrospection()
               .SetClientId(builder.Configuration["OpenIddict:IntrospectionClientId"])
               .SetClientSecret(builder.Configuration["OpenIddict:IntrospectionClientSecret"]);

        // Use system HttpClient
        options.UseSystemNetHttp();

        // Register the ASP.NET Core host
        options.UseAspNetCore();
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
    options.AddPolicy("Anonymous", policy =>
        policy.RequireAssertion(_ => true));
});

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builder => builder.AddRequestTransform(async context =>
    {
        // Forward the authorization header if present
        if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = context.HttpContext.Request.Headers["Authorization"].ToString();
            context.ProxyRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
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

// Add automatic OpenTelemetry filters for business context
builder.Services.AddOpenTelemetryFilters();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Problem Details middleware is enabled automatically with AddProblemDetails()

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
    .RequireAuthorization("Anonymous");

app.Run();
