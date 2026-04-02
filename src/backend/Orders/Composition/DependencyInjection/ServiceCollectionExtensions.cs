using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenIddict.Validation.AspNetCore;
using Orders.Configuration;
using Orders.Data;
using Orders.Data.Repositories;
using Orders.Data.Repositories.Interfaces;
using Orders.Services;
using Orders.Services.Interfaces;
using Orders.Validators;
using Shared.Authorization.Extensions;
using Shared.Authorization.Models;
using Shared.Logging.Extensions;

namespace Orders.Composition.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance = context.HttpContext.Request.Path;

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

        services.AddDbContext<OrdersDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
            options.UseNpgsql(connectionString);
        });

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                policy.SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
                    .WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        if (!environment.IsEnvironment("IntegrationTest"))
        {
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    var authority = ResolveAuthority(configuration);

                    options.SetIssuer(authority);
                    options.Configure(validation => validation.TokenValidationParameters.ValidateAudience = false);
                    options.UseAspNetCore();
                    options.UseSystemNetHttp().SetProductInformation(typeof(Program).Assembly);
                });

            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        }

        services.AddSingleton<IValidateOptions<AuthorizationConfig>, AuthorizationConfigValidator>();
        services.AddOptions<AuthorizationConfig>()
            .Bind(configuration.GetSection("Authorization"))
            .ValidateOnStart();

        services.AddConfigurablePolicies(configuration);
        services.Configure<AuthConfiguration>(configuration.GetSection("Authentication"));

        services.AddScoped<IOrdersService, OrdersService>();
        services.AddScoped<IIdempotencyService, IdempotencyService>();
        services.AddScoped<IOrdersRepository, OrdersRepository>();
        services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>(ServiceLifetime.Scoped);

        services.AddLoggingServices();

        return services;
    }

    private static Uri ResolveAuthority(IConfiguration configuration)
    {
        var configuredAuthority = configuration["Authentication:Authority"];
        if (!string.IsNullOrWhiteSpace(configuredAuthority)
            && !configuredAuthority.Contains("${", StringComparison.Ordinal)
            && Uri.TryCreate(configuredAuthority, UriKind.Absolute, out var authority))
        {
            return authority;
        }

        var externalAuthority = configuration["EXTERNAL_AUTH_URL"];
        if (!string.IsNullOrWhiteSpace(externalAuthority)
            && Uri.TryCreate(externalAuthority, UriKind.Absolute, out authority))
        {
            return authority;
        }

        throw new ArgumentNullException(
            "Authentication:Authority",
            "Authentication:Authority or EXTERNAL_AUTH_URL must be configured as an absolute URI.");
    }
}
