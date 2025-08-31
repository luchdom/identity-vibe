using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Shared.OpenTelemetry.Filters;

namespace Shared.OpenTelemetry.Extensions;

public static class OpenTelemetryExtensions
{
    public static WebApplicationBuilder AddOpenTelemetryAutoInstrumentation(this WebApplicationBuilder builder, string serviceName, string serviceVersion = "1.0.0")
    {
        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: serviceVersion)
            .AddAttributes(new[]
            {
                new KeyValuePair<string, object>("service.namespace", "identity-system"),
                new KeyValuePair<string, object>("deployment.environment", builder.Environment.EnvironmentName),
                new KeyValuePair<string, object>("service.instance.id", Environment.MachineName)
            });

        // Configure OpenTelemetry
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource = resourceBuilder)
            .WithTracing(tracerProviderBuilder =>
            {
                // Automatic ASP.NET Core instrumentation - captures all endpoints, exceptions, and metrics
                tracerProviderBuilder
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        // Automatically record exceptions  
                        // options.RecordException = true; // This property doesn't exist in this version
                        
                        // Filter out health check and metrics endpoints from traces
                        options.Filter = httpContext =>
                        {
                            var path = httpContext.Request.Path.Value?.ToLower();
                            return !path?.Contains("/health") == true && 
                                   !path?.Contains("/metrics") == true;
                        };
                        
                        // Automatically enrich traces with business context
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            var userAgent = request.Headers.UserAgent.ToString();
                            if (!string.IsNullOrEmpty(userAgent))
                            {
                                activity.SetTag("http.request.header.user_agent", userAgent);
                            }
                            var correlationId = request.Headers["X-Correlation-ID"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(correlationId))
                            {
                                activity.SetTag("http.request.header.correlation_id", correlationId);
                            }
                            
                            // Add user context if available
                            if (request.HttpContext.User?.Identity?.IsAuthenticated == true)
                            {
                                activity.SetTag("user.id", request.HttpContext.User.Identity.Name);
                                activity.SetTag("auth.authenticated", true);
                            }
                        };
                        
                        options.EnrichWithHttpResponse = (activity, response) =>
                        {
                            var correlationId = response.Headers["X-Correlation-ID"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(correlationId))
                            {
                                activity.SetTag("http.response.header.correlation_id", correlationId);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        // Automatically capture outgoing HTTP calls
                        // options.RecordException = true; // This property doesn't exist in this version
                        options.EnrichWithHttpRequestMessage = (activity, request) =>
                        {
                            activity.SetTag("http.client.request.header.correlation_id", 
                                request.Headers.GetValues("X-Correlation-ID").FirstOrDefault());
                        };
                        options.EnrichWithHttpResponseMessage = (activity, response) =>
                        {
                            if (response?.Headers != null && response.Headers.TryGetValues("X-Correlation-ID", out var values))
                            {
                                activity.SetTag("http.client.response.header.correlation_id", values.FirstOrDefault());
                            }
                        };
                    });

                // Add Entity Framework instrumentation for AuthServer (automatic database tracing)
                if (serviceName == "authserver")
                {
                    tracerProviderBuilder.AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.EnrichWithIDbCommand = (activity, command) =>
                        {
                            // Sanitize connection string for security
                            var connectionString = command.Connection?.ConnectionString;
                            if (!string.IsNullOrEmpty(connectionString))
                            {
                                activity.SetTag("db.connection_string.sanitized", SanitizeConnectionString(connectionString));
                            }
                        };
                    });
                    
                    tracerProviderBuilder.AddSqlClientInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        options.RecordException = true;
                    });
                }

                // Configure OTLP exporter
                var otlpEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT")
                    ?? builder.Configuration.GetValue<string>("OpenTelemetry:Endpoint")
                    ?? "http://localhost:4317";

                tracerProviderBuilder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });

                // Add console exporter for development debugging
                if (builder.Environment.IsDevelopment())
                {
                    tracerProviderBuilder.AddConsoleExporter();
                }
            })
            .WithMetrics(meterProviderBuilder =>
            {
                // Automatic metrics collection for ASP.NET Core
                meterProviderBuilder
                    .AddAspNetCoreInstrumentation() // HTTP request metrics (duration, count, etc.)
                    .AddHttpClientInstrumentation() // HTTP client metrics
                    .AddRuntimeInstrumentation()     // .NET runtime metrics (GC, memory, etc.)
                    .AddProcessInstrumentation();    // Process metrics (CPU, memory usage)

                // Configure OTLP exporter for metrics
                var otlpEndpoint = builder.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT")
                    ?? builder.Configuration.GetValue<string>("OpenTelemetry:Endpoint")
                    ?? "http://localhost:4317";

                meterProviderBuilder.AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                    options.Protocol = OtlpExportProtocol.Grpc;
                });

                // Add console exporter for development debugging
                if (builder.Environment.IsDevelopment())
                {
                    meterProviderBuilder.AddConsoleExporter();
                }
            });

        // Configure Serilog to send logs to OpenTelemetry
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
                // .Enrich.WithSpan()  // Add span enrichment for correlation - WithSpan not available in this version
                .WriteTo.Console()
                .WriteTo.File(
                    path: GetLogFilePath(context.Configuration, serviceName),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100 * 1024 * 1024,
                    rollOnFileSizeLimit: true
                );

            // Add OpenTelemetry sink
            var otlpEndpoint = context.Configuration.GetValue<string>("OTEL_EXPORTER_OTLP_ENDPOINT")
                ?? context.Configuration.GetValue<string>("OpenTelemetry:Endpoint")
                ?? "http://localhost:4318";

            configuration.WriteTo.OpenTelemetry(options =>
            {
                options.Endpoint = $"{otlpEndpoint.Replace("4317", "4318")}/v1/logs";
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName,
                    ["service.version"] = serviceVersion,
                    ["service.namespace"] = "identity-system"
                };
            });

            // Environment-specific configuration
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.MinimumLevel.Debug();
            }
            else
            {
                configuration.MinimumLevel.Information();
            }

            // Override specific log levels
            configuration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("OpenTelemetry", LogEventLevel.Warning);
        });

        // Register automatic OpenTelemetry filters for business context enrichment
        builder.Services.AddScoped<OpenTelemetryActionFilter>();
        builder.Services.AddScoped<OpenTelemetryExceptionFilter>();

        return builder;
    }

    public static IServiceCollection AddOpenTelemetryFilters(this IServiceCollection services)
    {
        // Add the filters to all controllers automatically
        services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
        {
            options.Filters.Add<OpenTelemetryActionFilter>();
            options.Filters.Add<OpenTelemetryExceptionFilter>();
        });

        return services;
    }

    public static WebApplication UseOpenTelemetryMiddleware(this WebApplication app)
    {
        // Enable W3C trace context propagation
        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        return app;
    }

    private static string GetLogFilePath(IConfiguration configuration, string serviceName)
    {
        var environment = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Development";
        var logDirectory = configuration["Logging:LogDirectory"] ?? "logs";
        
        // Ensure directory exists
        Directory.CreateDirectory(logDirectory);
        
        return Path.Combine(logDirectory, $"{serviceName.ToLower()}-{environment.ToLower()}.json");
    }

    private static string? SanitizeConnectionString(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return null;

        // Remove sensitive information from connection string
        var parts = connectionString.Split(';')
            .Where(part => !part.Trim().StartsWith("Password", StringComparison.OrdinalIgnoreCase) 
                          && !part.Trim().StartsWith("Pwd", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return string.Join(";", parts);
    }
}