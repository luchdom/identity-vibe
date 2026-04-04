using Orders.Models.Enums;

namespace Orders.Models.Requests;

public sealed record UpdateOrderStatusRequest(OrderStatus Status, string? Reason, string? Notes);
