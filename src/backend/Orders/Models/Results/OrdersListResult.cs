namespace Orders.Models.Results;

public sealed record OrdersListResult
{
    public required IReadOnlyList<OrderDetailsResult> Orders { get; init; }
    public required PaginationResult Pagination { get; init; }
    public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;
}
