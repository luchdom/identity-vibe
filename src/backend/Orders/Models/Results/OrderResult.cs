using Orders.Models.ViewModels;

namespace Orders.Models.Results;

public record OrderResult
{
    public required OrderData Order { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}