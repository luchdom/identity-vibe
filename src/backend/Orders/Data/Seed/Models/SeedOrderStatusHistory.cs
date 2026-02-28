using Orders.Models.Enums;

namespace Orders.Data.Seed.Models;

public sealed record SeedOrderStatusHistory
{
    public OrderStatus FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
    public required string ChangedByUserId { get; init; }
    public string? ChangedByUserName { get; init; }
    public required DateTime CreatedAt { get; init; }
}
