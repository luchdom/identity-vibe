namespace Orders.Models.Queries;

public record GetOrderByIdQuery
{
    public required int OrderId { get; init; }
    public required string UserId { get; init; }
    public bool IsAdminRequest { get; init; }
}