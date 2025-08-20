using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using AuthServer.Data;
using Microsoft.AspNetCore.Identity;
using AuthServer.Configuration;
using AuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
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
               .EnableAuthorizationEndpointPassthrough();

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
            // Domain-based user scopes
            "data.read",
            "data.write", 
            "data.delete",
            "profile.read",
            "profile.write",
            "admin.manage",
            // Internal service scopes
            "internal.servicea.read",
            "internal.servicea.create",
            "internal.servicea.update",
            "internal.servicea.delete",
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

app.UseCors("DefaultPolicy");

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
