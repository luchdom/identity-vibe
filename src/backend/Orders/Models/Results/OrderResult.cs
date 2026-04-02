namespace Orders.Models.Results;

public sealed record OrderResult
{
    public required OrderDetailsResult Order { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
