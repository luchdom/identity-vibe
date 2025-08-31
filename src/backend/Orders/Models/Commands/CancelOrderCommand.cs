namespace Orders.Models.Commands;

public record CancelOrderCommand
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public required string? CorrelationId { get; init; }
    public string? CancellationReason { get; init; }
    public string? Notes { get; init; }
}