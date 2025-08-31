namespace Orders.Models.Commands;

public record CreateOrderCommand
{
    public required string UserId { get; init; }
    public string? CorrelationId { get; init; }
    public required CustomerCommand Customer { get; init; }
    public required List<OrderItemCommand> Items { get; init; }
    public ShippingCommand? Shipping { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? IdempotencyKey { get; init; }
}