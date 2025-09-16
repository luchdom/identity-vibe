using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthServer.Data;
using AuthServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using AuthServer.Configuration;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
using AuthServer.Repositories;
using AuthServer.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Load scope configuration
builder.Configuration.AddJsonFile("appsettings.Scopes.json", optional: false, reloadOnChange: true);
// Load client configuration
builder.Configuration.AddJsonFile("appsettings.Clients.json", optional: false, reloadOnChange: true);

// Configure automatic OpenTelemetry instrumentation
builder.AddOpenTelemetryAutoInstrumentation("authserver", "1.0.0");

// Add services to the container.
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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (builder.Environment.EnvironmentName == "Docker")
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
    // Register OpenIddict entities
    options.UseOpenIddict();
});

// Add Identity
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // For demo purposes
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .Build();
});

// Configure scope options
builder.Services.Configure<ScopeConfiguration>(
    builder.Configuration.GetSection("ScopeConfiguration"));
// Configure client options
builder.Services.Configure<ClientConfiguration>(
    builder.Configuration);

// Register services
builder.Services.AddScoped<ScopeConfigurationService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Clean Architecture Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();

// Clean Architecture Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Add logging services
builder.Services.AddLoggingServices();

// Add automatic OpenTelemetry filters for business context
builder.Services.AddOpenTelemetryFilters();

// Add OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        // Configure issuer URL from environment variable
        var externalAuthUrl = builder.Configuration["EXTERNAL_AUTH_URL"] ?? "https://localhost:5000";
        options.SetIssuer(new Uri(externalAuthUrl));

        options
            .SetTokenEndpointUris("/connect/token");

        // Enable the required flows
        options.AllowPasswordFlow()
               .AllowClientCredentialsFlow()
               .AllowRefreshTokenFlow();

        // Accept anonymous clients (i.e clients that don't send a client_id).
        options.AcceptAnonymousClients();

        // Register the signing and encryption credentials
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options
        // Configure JWT tokens
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();

        // Disable access token encryption for easier integration with third-party APIs
        options.DisableAccessTokenEncryption();

        // Configure scopes from configuration
        // Note: We can't use DI here as the container isn't built yet
        // The scopes will be properly registered from configuration during database seeding
        // For now, register the standard OpenIddict scopes and custom scopes
        var scopesToRegister = new List<string>
        {
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.OfflineAccess,
            OpenIddictConstants.Scopes.Roles
        };

        // Add custom scopes from configuration if available
        var scopeConfigSection = builder.Configuration.GetSection("ScopeConfiguration:Scopes");
        if (scopeConfigSection.Exists())
        {
            var customScopes = scopeConfigSection.Get<List<ScopeSettings>>();
            if (customScopes != null)
            {
                scopesToRegister.AddRange(customScopes.Select(s => s.Name));
            }
        }
        else
        {
            // Fallback to hardcoded scopes if configuration is missing
            scopesToRegister.AddRange(new[]
            {
                "orders.read", "orders.write", "orders.manage",
                "profile.read", "profile.write",
                "admin.manage", "admin.users", "admin.roles",
                "gateway-bff"
            });
        }

        options.RegisterScopes(scopesToRegister.Distinct().ToArray());
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Problem Details middleware is enabled automatically with AddProblemDetails()

app.UseRouting();

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Add OpenTelemetry middleware
app.UseOpenTelemetryMiddleware();

// Add logging middleware
app.UseLoggingMiddleware();
app.UseSerilogMiddleware();

app.MapControllers();

// Seed the database with default user and roles
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
