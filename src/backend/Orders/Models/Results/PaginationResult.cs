namespace Orders.Models.Results;

public sealed record PaginationResult(
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);
