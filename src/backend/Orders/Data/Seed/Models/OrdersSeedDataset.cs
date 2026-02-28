namespace Orders.Data.Seed.Models;

public sealed record OrdersSeedDataset
{
    public required int Version { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public string? Description { get; init; }
    public required List<SeedOrder> Orders { get; init; }
}
