namespace Orders.Models.Requests;

public sealed record CustomerInfo(
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? Address,
    string? City,
    string? State,
    string? PostalCode,
    string? Country);
