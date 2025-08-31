namespace Orders.Models.Commands;

public record UpdateOrderCommand
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public string? CorrelationId { get; init; }
    public CustomerCommand? Customer { get; init; }
    public List<OrderItemCommand>? Items { get; init; }
    public ShippingCommand? Shipping { get; init; }
    public string? SpecialInstructions { get; init; }
    public string? IdempotencyKey { get; init; }
}