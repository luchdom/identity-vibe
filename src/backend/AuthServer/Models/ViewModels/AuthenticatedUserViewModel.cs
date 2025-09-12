namespace AuthServer.Models.ViewModels;

public record AuthenticatedUserViewModel
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string[] Roles { get; init; } = [];
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
}