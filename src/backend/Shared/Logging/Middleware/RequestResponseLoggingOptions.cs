namespace Shared.Logging.Middleware;

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
