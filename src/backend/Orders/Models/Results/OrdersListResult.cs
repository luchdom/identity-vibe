using Orders.Models.ViewModels;

namespace Orders.Models.Results;

public record OrdersListResult
{
    public required List<OrderViewModel> Orders { get; init; }
    public required PaginationViewModel Pagination { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}