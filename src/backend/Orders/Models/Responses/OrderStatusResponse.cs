using Orders.Models.Enums;

namespace Orders.Models.Responses;

public record OrderStatusResponse
{
    public int OrderId { get; init; }
    public required string OrderNumber { get; init; }
    public OrderStatus CurrentStatus { get; init; }
    public string StatusDisplay => CurrentStatus.ToString();
    public string? TrackingNumber { get; init; }
    public DateTime LastUpdated { get; init; }
    public required List<OrderStatusHistoryResponse> StatusHistory { get; init; }
}