using Orders.Models.Enums;

namespace Orders.Models.ViewModels;

public record OrderStatusData
{
    public int OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public OrderStatus Status { get; init; }
    public string? TrackingNumber { get; init; }
    public DateTime LastUpdated { get; init; }
    public required List<OrderStatusHistoryData> StatusHistory { get; init; }
}