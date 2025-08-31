using Orders.Models.Enums;

namespace Orders.Models.Queries;

public record GetOrdersQuery
{
    public required string UserId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public OrderStatus? Status { get; init; }
}