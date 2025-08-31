using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text;
using Shared.Logging.Services;

namespace Shared.Logging.Middleware;

public class RequestResponseLoggingMiddleware(
    RequestDelegate next, 
    ILogger<RequestResponseLoggingMiddleware> logger,
    IOptions<RequestResponseLoggingOptions> options)
{
    private readonly RequestResponseLoggingOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context, ICorrelationIdService correlationIdService)
    {
        if (ShouldSkipLogging(context))
        {
            await next(context);
            return;
        }

        var correlationId = correlationIdService.GetOrGenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        await LogRequestAsync(context, correlationId);
        
        // Capture response
        var originalResponseStream = context.Response.Body;
        using var responseStream = new MemoryStream();
        context.Response.Body = responseStream;
        
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Request failed for {Method} {Path} with CorrelationId {CorrelationId}",
                context.Request.Method, 
                context.Request.Path, 
                correlationId);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            
            // Copy response back to original stream
            responseStream.Seek(0, SeekOrigin.Begin);
            await responseStream.CopyToAsync(originalResponseStream);
        }
    }

    private bool ShouldSkipLogging(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        
        return _options.ExcludedPaths.Any(excludedPath => 
            path != null && path.Contains(excludedPath.ToLowerInvariant()));
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        var requestSize = request.ContentLength ?? 0;
        
        var requestLog = new
        {
            CorrelationId = correlationId,
            Method = request.Method,
            Path = request.Path.Value,
            QueryString = request.QueryString.Value,
            Headers = GetFilteredHeaders(request.Headers),
            ContentType = request.ContentType,
            ContentLength = requestSize,
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            UserAgent = request.Headers.UserAgent.FirstOrDefault(),
            Scheme = request.Scheme,
            Host = request.Host.Value
        };

        logger.LogInformation(
            "HTTP Request: {Method} {Path}{QueryString} from {RemoteIpAddress} | Size: {ContentLength} bytes | CorrelationId: {CorrelationId}",
            request.Method,
            request.Path,
            request.QueryString,
            context.Connection.RemoteIpAddress,
            requestSize,
            correlationId);

        if (_options.LogRequestBody && requestSize > 0 && requestSize <= _options.MaxBodySizeToLog)
        {
            var requestBody = await ReadRequestBodyAsync(request);
            if (!string.IsNullOrWhiteSpace(requestBody))
            {
                logger.LogDebug(
                    "Request Body for {CorrelationId}: {RequestBody}",
                    correlationId,
                    requestBody);
            }
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var response = context.Response;
        var responseSize = response.Body.Length;
        
        var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
        
        logger.Log(logLevel,
            "HTTP Response: {StatusCode} for {Method} {Path} | Size: {ContentLength} bytes | Duration: {ElapsedMilliseconds}ms | CorrelationId: {CorrelationId}",
            response.StatusCode,
            context.Request.Method,
            context.Request.Path,
            responseSize,
            elapsedMilliseconds,
            correlationId);

        if (_options.LogResponseBody && responseSize > 0 && responseSize <= _options.MaxBodySizeToLog)
        {
            var responseBody = await ReadResponseBodyAsync(response);
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                logger.LogDebug(
                    "Response Body for {CorrelationId}: {ResponseBody}",
                    correlationId,
                    responseBody);
            }
        }

        // Log slow requests
        if (elapsedMilliseconds > _options.SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Slow Request: {Method} {Path} took {ElapsedMilliseconds}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                elapsedMilliseconds,
                correlationId);
        }
    }

    private async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanRead)
            return string.Empty;

        request.EnableBuffering();
        request.Body.Position = 0;
        
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Position = 0;
        
        return body;
    }

    private async Task<string> ReadResponseBodyAsync(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin);
        
        return body;
    }

    private Dictionary<string, string> GetFilteredHeaders(IHeaderDictionary headers)
    {
        var filteredHeaders = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            if (_options.SensitiveHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                filteredHeaders[header.Key] = "[REDACTED]";
            }
            else
            {
                filteredHeaders[header.Key] = string.Join(", ", header.Value!);
            }
        }
        
        return filteredHeaders;
    }
}

public class RequestResponseLoggingOptions
{
    public bool LogRequestBody { get; set; } = false;
    public bool LogResponseBody { get; set; } = false;
    public long MaxBodySizeToLog { get; set; } = 4096; // 4KB
    public int SlowRequestThresholdMs { get; set; } = 5000; // 5 seconds
    
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/metrics",
        "/swagger",
        "/favicon.ico",
        "/_framework",
        "/_vs/browserLink"
    };
    
    public List<string> SensitiveHeaders { get; set; } = new()
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-API-Key",
        "X-Auth-Token"
    };
}