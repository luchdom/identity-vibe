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

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddOpenIdConnect();


// // Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .Build();
});

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builder => builder.AddRequestTransform(async context =>
    {
        // Attach the access token retrieved from the authentication cookie.
        //
        // Note: in a real world application, the expiration date of the access token
        // should be checked before sending a request to avoid getting a 401 response.
        // Once expired, a new access token could be retrieved using the OAuth 2.0
        // refresh token grant (which could be done transparently).
        var token = await context.HttpContext.GetTokenAsync("access_token");

        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }));;

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

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();
