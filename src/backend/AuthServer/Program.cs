using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using AuthServer.Configuration;
using AuthServer.Services;
using AuthServer.Services.Interfaces;
using AuthServer.Repositories;
using AuthServer.Repositories.Interfaces;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

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

// Configure scope options
builder.Services.Configure<ScopeConfiguration>(
    builder.Configuration.GetSection("ScopeConfiguration"));

// Register services
builder.Services.AddScoped<ScopeConfigurationService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Clean Architecture Services
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();

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
        options
            .SetTokenEndpointUris("/connect/token")
            .SetAuthorizationEndpointUris("/connect/authorize")
            .SetIntrospectionEndpointUris("/connect/introspect");

        // Enable the required flows
        options.AllowPasswordFlow()
               .AllowClientCredentialsFlow()
               .AllowRefreshTokenFlow();

        // Register the signing and encryption credentials
        options.AddDevelopmentEncryptionCertificate()
               .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the ASP.NET Core options
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableAuthorizationEndpointPassthrough()
               .EnableConfigurationEndpointPassthrough();

        // Configure JWT tokens
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .DisableTransportSecurityRequirement();

        // Configure the JWT handler
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey()
               .DisableAccessTokenEncryption();

        // Configure scopes
        options.RegisterScopes(
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            // Orders-specific scopes
            "orders.read",
            "orders.write",
            "orders.manage",
            // Profile scopes
            "profile.read",
            "profile.write",
            // Admin scopes
            "admin.manage",
            "admin.users",
            "admin.roles",
            // Internal service scopes
            "internal.orders.read",
            "internal.orders.write",
            "internal.orders.manage",
            "gateway-bff"
        );
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

app.UseCors("DefaultPolicy");

// Add OpenTelemetry middleware
app.UseOpenTelemetryMiddleware();

// Add logging middleware
app.UseLoggingMiddleware();
app.UseSerilogMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed the database with default user and roles
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();
