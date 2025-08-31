using Orders.Models.ViewModels;

namespace Orders.Models.Results;

public record OrdersListResult
{
    public required List<OrderData> Orders { get; init; }
    public required PaginationData Pagination { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}