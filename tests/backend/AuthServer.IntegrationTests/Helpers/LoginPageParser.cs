using AngleSharp;

namespace AuthServer.IntegrationTests.Helpers;

public sealed record LoginPageForm(string AntiForgeryToken, string? ReturnUrl);

public static class LoginPageParser
{
    public static async Task<LoginPageForm> ParseAsync(string html)
    {
        var context = BrowsingContext.New(AngleSharp.Configuration.Default);
        var document = await context.OpenAsync(req => req.Content(html));

        var antiForgeryToken = document.QuerySelector("input[name='__RequestVerificationToken']")?.GetAttribute("value");
        if (string.IsNullOrWhiteSpace(antiForgeryToken))
        {
            throw new InvalidOperationException("Anti-forgery token input '__RequestVerificationToken' was not found in the login page.");
        }

        var returnUrl = document.QuerySelector("input[name='ReturnUrl']")?.GetAttribute("value");
        return new LoginPageForm(antiForgeryToken, returnUrl);
    }
}
