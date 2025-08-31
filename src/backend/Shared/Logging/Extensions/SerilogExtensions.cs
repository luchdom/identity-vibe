using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Shared.Logging.Services;
using Shared.Logging.Middleware;

namespace Shared.Logging.Extensions;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Remove default logging providers
        builder.Logging.ClearProviders();
        
        // Configure Serilog
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithCorrelationId()
                .WriteTo.Console(new JsonFormatter())
                .WriteTo.File(
                    new JsonFormatter(),
                    path: GetLogFilePath(context.Configuration, context.HostingEnvironment.ApplicationName),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100 * 1024 * 1024, // 100MB
                    rollOnFileSizeLimit: true
                );

            // Override specific log levels
            configuration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning);

            // Environment-specific configuration
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug();
            }
            else if (context.HostingEnvironment.IsProduction())
            {
                configuration.MinimumLevel.Information();
            }
        });

        return builder;
    }

    public static WebApplication UseSerilogMiddleware(this WebApplication app)
    {
        // Add Serilog request logging (built-in)
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.FirstOrDefault());
            };
        });

        return app;
    }

    public static IServiceCollection AddLoggingServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdService, CorrelationIdService>();
        
        // Configure request/response logging options
        services.Configure<RequestResponseLoggingOptions>(options =>
        {
            // Default configuration - can be overridden in appsettings
            options.LogRequestBody = false;
            options.LogResponseBody = false;
            options.MaxBodySizeToLog = 4096;
            options.SlowRequestThresholdMs = 5000;
        });

        return services;
    }

    public static WebApplication UseLoggingMiddleware(this WebApplication app)
    {
        // Add correlation ID middleware first
        app.UseMiddleware<CorrelationIdMiddleware>();
        
        // Add request/response logging middleware
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

        return app;
    }

    private static string GetLogFilePath(IConfiguration configuration, string applicationName)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var logDirectory = configuration["Logging:LogDirectory"] ?? "logs";
        
        // Ensure directory exists
        Directory.CreateDirectory(logDirectory);
        
        return Path.Combine(logDirectory, $"{applicationName.ToLower()}-{environment.ToLower()}.json");
    }
}