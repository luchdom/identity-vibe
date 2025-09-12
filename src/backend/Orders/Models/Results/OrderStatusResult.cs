using Orders.Models.ViewModels;

namespace Orders.Models.Results;

public record OrderStatusResult
{
    public required OrderStatusViewModel StatusData { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}