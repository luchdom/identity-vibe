using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record CancelOrderRequest
{
    [MaxLength(500)]
    public string? CancellationReason { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }

    [MaxLength(100)]
    public string? IdempotencyKey { get; init; }
}