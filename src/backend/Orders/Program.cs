using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using Orders.Configuration;
using Orders.Data;
using Orders.Middleware;
using Orders.Repositories;
using Orders.Repositories.Interfaces;
using Orders.Services;
using Orders.Services.Interfaces;
using Shared.Authorization.Extensions;
using Shared.Authorization.Models;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure automatic OpenTelemetry instrumentation
builder.AddOpenTelemetryAutoInstrumentation("orders", "1.0.0");

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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
    options.UseNpgsql(connectionString);
});

// Configure CORS
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
        options.SetIssuer(authority);

        options.Configure(options => options.TokenValidationParameters.ValidateAudience = false);

        // Register the ASP.NET Core host
        options.UseAspNetCore();

        // Configure caching for discovery document
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly);

        // NO call to UseIntrospection() = Local JWT validation
    });

// Add authentication
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

builder.Services.AddSingleton<IValidateOptions<AuthorizationConfig>, AuthorizationConfigValidator>();
builder.Services.AddOptions<AuthorizationConfig>()
    .Bind(builder.Configuration.GetSection("Authorization"))
    .ValidateOnStart();

// Configure authorization with configurable policies
builder.Services.AddConfigurablePolicies(builder.Configuration);

// Configure options
builder.Services.Configure<AuthConfiguration>(
    builder.Configuration.GetSection("Authentication"));

// Clean Architecture Services
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// Clean Architecture Repositories
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();

// Add logging services
builder.Services.AddLoggingServices();


var app = builder.Build();

// Configure the HTTP request pipeline.
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

// Add idempotency middleware
app.UseMiddleware<IdempotencyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
    await context.Database.MigrateAsync();

    // Seed sample data
    var seeder = new DatabaseSeeder(context, app.Environment.ContentRootPath, logger);
    await seeder.SeedAsync();
}

app.MapControllers();

// Health check endpoint (anonymous - no auth required)
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .AllowAnonymous();

app.Run();
