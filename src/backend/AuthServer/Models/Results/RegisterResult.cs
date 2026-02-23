using AuthServer.Models.ViewModels;

namespace AuthServer.Models.Results;

public class RegisterResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserViewModel? User { get; set; }
}
