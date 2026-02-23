using AuthServer.Models.ViewModels;

namespace AuthServer.Models.Results;

public class LoginResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserViewModel? User { get; set; }
}
