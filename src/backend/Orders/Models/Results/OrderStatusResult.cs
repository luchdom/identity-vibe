namespace Orders.Models.Results;

public sealed record OrderStatusResult
{
    public required OrderStatusDetailsResult StatusData { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
