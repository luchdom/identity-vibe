namespace AuthServer.Configuration;

public class ClientSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string? ClientSecret { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "Public";
    public List<string> RedirectUris { get; set; } = new();
    public List<string> PostLogoutRedirectUris { get; set; } = new();
    public List<string> AllowedGrantTypes { get; set; } = new();
    public List<string> AllowedScopes { get; set; } = new();
    public bool RequireConsent { get; set; } = false;
    public bool RequirePkce { get; set; } = true;
}