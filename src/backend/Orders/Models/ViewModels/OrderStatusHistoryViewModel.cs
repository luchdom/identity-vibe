using Orders.Models.Enums;

namespace Orders.Models.ViewModels;

public record OrderStatusHistoryViewModel
{
    public int Id { get; init; }
    public int OrderId { get; init; }
    public OrderStatus FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public required string ChangedByUserId { get; init; }
    public string? CorrelationId { get; init; }
    public DateTime CreatedAt { get; init; }
}