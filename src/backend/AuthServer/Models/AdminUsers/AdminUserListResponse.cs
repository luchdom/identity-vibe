namespace AuthServer.Models.AdminUsers;

public sealed record AdminUserListResponse
{
    public required IReadOnlyList<AdminUserSummaryResponse> Users { get; init; }
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasPreviousPage { get; init; }
    public bool HasNextPage { get; init; }
}
