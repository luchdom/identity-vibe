using Orders.Models.Enums;

namespace Orders.Models.Queries;

public record GetAllOrdersQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public OrderStatus? Status { get; init; }
}