using System.ComponentModel.DataAnnotations;

namespace Orders.Models.Requests;

public record AddTrackingNumberRequest
{
    [Required]
    [MaxLength(100)]
    public required string TrackingNumber { get; init; }
}