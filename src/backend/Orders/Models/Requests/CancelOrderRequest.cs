namespace Orders.Models.Requests;

public sealed record CancelOrderRequest(
    string? CancellationReason = null,
    string? Notes = null,
    string? IdempotencyKey = null);
