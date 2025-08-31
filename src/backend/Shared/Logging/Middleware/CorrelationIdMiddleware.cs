using Shared.Logging.Services;

namespace Shared.Logging.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        var correlationId = correlationIdService.GetOrGenerateCorrelationId();
        
        // Enrich the logging context with correlation ID
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        {
            logger.LogDebug("Processing request with CorrelationId: {CorrelationId}", correlationId);
            
            // Ensure correlation ID is set in response headers
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }
            
            await next(context);
        }
    }
}