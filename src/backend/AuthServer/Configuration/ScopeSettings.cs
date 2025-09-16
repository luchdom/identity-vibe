namespace AuthServer.Configuration;

public class ScopeSettings
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = new();
}