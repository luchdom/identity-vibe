namespace Orders.Models.Results;

public sealed record CustomerResult(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? PostalCode,
    string? Country,
    DateTime CreatedAt,
    DateTime UpdatedAt);
