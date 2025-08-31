using Orders.Models.Enums;

namespace Orders.Models.Responses;

public record OrderStatusHistoryResponse
{
    public int Id { get; init; }
    public OrderStatus FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public required string StatusChange { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public required string ChangedByUserId { get; init; }
    public string? ChangedByUserName { get; init; }
    public DateTime CreatedAt { get; init; }
}