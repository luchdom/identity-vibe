using Orders.Models.ViewModels;

namespace Orders.Models.Results;

public record OrderStatusResult
{
    public required OrderStatusData StatusData { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}