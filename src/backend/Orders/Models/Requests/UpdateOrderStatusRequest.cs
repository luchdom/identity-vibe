using System.ComponentModel.DataAnnotations;
using Orders.Models.Enums;

namespace Orders.Models.Requests;

public record UpdateOrderStatusRequest
{
    [Required]
    public required OrderStatus Status { get; init; }
    
    public string? Reason { get; init; }
    public string? Notes { get; init; }
}