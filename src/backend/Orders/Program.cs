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

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<OrdersDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (builder.Environment.EnvironmentName == "Docker")
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
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

// Configure authorization with configuration-driven policies
var authService = new AuthorizationService(builder.Configuration);
authService.RegisterPolicies(builder.Services);

// Configure options
builder.Services.Configure<AuthConfiguration>(
    builder.Configuration.GetSection("Auth"));

// Clean Architecture Services
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// Clean Architecture Repositories
builder.Services.AddScoped<IOrdersRepository, OrdersRepository>();

// Add logging services
builder.Services.AddLoggingServices();

// Add automatic OpenTelemetry filters for business context
builder.Services.AddOpenTelemetryFilters();

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

// Add idempotency middleware
app.UseMiddleware<IdempotencyMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await context.Database.EnsureCreatedAsync();

    // Seed sample data
    var seeder = new DatabaseSeeder(context);
    await seeder.SeedAsync();
}

app.MapControllers();

app.Run();
