using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Logging.Extensions;
using Shared.OpenTelemetry.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Configure automatic OpenTelemetry instrumentation
builder.AddOpenTelemetryAutoInstrumentation("gateway", "1.0.0");

// Add CORS
// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("DefaultPolicy", policy =>
//     {
//         var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
//         policy.WithOrigins(allowedOrigins)
//               .AllowAnyMethod()
//               .AllowAnyHeader()
//               .AllowCredentials();
//     });
// });

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


// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var authority = builder.Configuration["Authentication:Authority"];
        var audience = builder.Configuration["Authentication:Audience"];

        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = false; // For development only

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false, // Keep disabled for multi-service scenarios
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = authority,
            ClockSkew = TimeSpan.FromMinutes(5),
            NameClaimType = "name",
            RoleClaimType = "role"
        };
    });

// Add Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AuthenticatedUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

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
