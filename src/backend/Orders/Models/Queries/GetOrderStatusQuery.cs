namespace Orders.Models.Queries;

public record GetOrderStatusQuery
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public bool IsAdminRequest { get; init; }
}