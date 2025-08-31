namespace Shared.Logging.Services;

public class CorrelationIdService : ICorrelationIdService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string CorrelationIdHeaderName = "X-Correlation-ID";
    private const string CorrelationIdKey = "CorrelationId";

    public CorrelationIdService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetOrGenerateCorrelationId()
    {
        var correlationId = GetCorrelationId();
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
            SetCorrelationId(correlationId);
        }
        return correlationId;
    }

    public string? GetCorrelationId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Check if already stored in HttpContext.Items
        if (context.Items.TryGetValue(CorrelationIdKey, out var storedId))
        {
            return storedId?.ToString();
        }

        // Check if provided in headers
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue))
        {
            var correlationId = headerValue.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                context.Items[CorrelationIdKey] = correlationId;
                return correlationId;
            }
        }

        return null;
    }

    public void SetCorrelationId(string correlationId)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            context.Items[CorrelationIdKey] = correlationId;
            
            // Add to response headers
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeaderName))
            {
                context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            }
        }
    }
}