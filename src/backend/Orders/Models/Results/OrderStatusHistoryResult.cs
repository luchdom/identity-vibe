using Orders.Models.Enums;

namespace Orders.Models.Results;

public sealed record OrderStatusHistoryResult(
    int Id,
    int OrderId,
    OrderStatus FromStatus,
    OrderStatus ToStatus,
    string? Reason,
    string? Notes,
    string ChangedByUserId,
    string? CorrelationId,
    DateTime CreatedAt);
