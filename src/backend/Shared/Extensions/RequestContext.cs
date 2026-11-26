namespace Shared.Extensions;

/// <summary>
/// Request context data for command mapping
/// </summary>
public record RequestContext
{
    public string? UserId { get; init; }
    public string? CorrelationId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsAdmin { get; init; }
}
