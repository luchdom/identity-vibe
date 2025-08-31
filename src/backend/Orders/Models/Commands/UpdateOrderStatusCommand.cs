using Orders.Models.Enums;

namespace Orders.Models.Commands;

public record UpdateOrderStatusCommand
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public required OrderStatus Status { get; init; }
    public string? Reason { get; init; }
    public string? Notes { get; init; }
}