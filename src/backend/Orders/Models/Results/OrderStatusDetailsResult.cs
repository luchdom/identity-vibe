using Orders.Models.Enums;

namespace Orders.Models.Results;

public sealed record OrderStatusDetailsResult(
    int OrderId,
    string OrderNumber,
    OrderStatus Status,
    string? TrackingNumber,
    DateTime LastUpdated,
    IReadOnlyList<OrderStatusHistoryResult> StatusHistory);
