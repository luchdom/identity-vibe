using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthServer.Data;
using AuthServer.Data.Entities;
using Microsoft.AspNetCore.Identity;
using AuthServer.Configuration;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using OpenIddict.Validation.AspNetCore;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;
using Microsoft.Extensions.Options;
using Serilog;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Load scope configuration
builder.Configuration.AddJsonFile("appsettings.Scopes.json", optional: false, reloadOnChange: true);
// Load client configuration
builder.Configuration.AddJsonFile("appsettings.Clients.json", optional: false, reloadOnChange: true);

// Add automatic OpenTelemetry filters for business context
// Configure automatic OpenTelemetry instrumentation
builder.AddOpenTelemetryAutoInstrumentation("authserver", "1.0.0", dbTelemetry: true);

// Add services to the container.
builder.Services.AddControllersWithViews();

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
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
        policy.WithOrigins(allowedOrigins)
            .SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add Entity Framework Core
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
   options.UseNpgsql(connectionString);

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

// Add cookie authentication for the authorization flow
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/connect/logout";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
        .Build();

    options.AddPolicy(AuthServer.Controllers.AdminRolesController.AdminRolesManagementPolicy, policy =>
        policy.RequireAuthenticatedUser()
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAssertion(ctx => ctx.User.HasAnyScope("admin.roles", "admin.full")));

    options.AddPolicy(AuthServer.Controllers.AdminUsersController.AdminUsersManagementPolicy, policy =>
        policy.RequireAuthenticatedUser()
            .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
            .RequireAssertion(ctx => ctx.User.HasAnyScope("admin.users", "admin.full")));
});

// Configure and validate options
builder.Services.AddSingleton<IValidateOptions<ScopeConfiguration>, ScopeConfigurationValidator>();
builder.Services.AddOptions<ScopeConfiguration>()
    .Bind(builder.Configuration.GetSection("ScopeConfiguration"))
    .ValidateOnStart();

builder.Services.AddSingleton<IValidateOptions<ClientConfiguration>, ClientConfigurationValidator>();
builder.Services.AddOptions<ClientConfiguration>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

// Register services
builder.Services.AddScoped<ScopeConfigurationService>();
builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<IAuthorizationGrantService, AuthorizationGrantService>();
builder.Services.AddScoped<IRoleAdminService, RoleAdminService>();
builder.Services.AddScoped<IUserAdminService, UserAdminService>();

// Clean Architecture Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Add logging services
builder.Services.AddLoggingServices();



// Add OpenIddict
builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<AppDbContext>();
    })
    .AddServer(options =>
    {
        // Configure endpoints
        options
            .SetAuthorizationEndpointUris("/connect/authorize")
            .SetTokenEndpointUris("/connect/token")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetUserInfoEndpointUris("/connect/userinfo")
            .SetEndSessionEndpointUris("/connect/logout");

        // Enable the required flows
        options.AllowAuthorizationCodeFlow()
               .RequireProofKeyForCodeExchange() // PKCE required
               .AllowClientCredentialsFlow()
               .AllowRefreshTokenFlow();

        // Register signing and encryption credentials.
        // Integration tests use ephemeral keys to avoid dev certificate store flakiness.
        if (builder.Environment.IsEnvironment("IntegrationTest"))
        {
            options.AddEphemeralEncryptionKey()
                   .AddEphemeralSigningKey();
        }
        else
        {
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
        }

        // Register the ASP.NET Core host and configure the ASP.NET Core options
        options.UseAspNetCore()
               .EnableAuthorizationEndpointPassthrough()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .EnableEndSessionEndpointPassthrough()
               .DisableTransportSecurityRequirement();

        // Use self-contained JWT access tokens for local validation
        // Keep reference refresh tokens for security

        if(builder.Environment.IsProduction())
        {
            options.DisableAccessTokenEncryption()
                .SetAccessTokenLifetime(TimeSpan.FromMinutes(10))
                .SetRefreshTokenLifetime(TimeSpan.FromMinutes(60));
        }
        else
        {
            options.DisableAccessTokenEncryption()
                .SetAccessTokenLifetime(TimeSpan.FromDays(30))
                .SetRefreshTokenLifetime(TimeSpan.FromDays(30));
        }

        // Configure scopes from configuration
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
app.UseStaticFiles();

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseOpenTelemetryMiddleware();

// Add logging middleware
app.UseLoggingMiddleware();
app.UseSerilogMiddleware();

app.MapDefaultControllerRoute();
app.MapControllers();

// Seed the database with default user and roles
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Health check endpoint (anonymous - no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();

public partial class Program { }
