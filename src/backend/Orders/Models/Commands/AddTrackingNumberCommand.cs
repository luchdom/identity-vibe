namespace Orders.Models.Commands;

public record AddTrackingNumberCommand
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public required string TrackingNumber { get; init; }
}