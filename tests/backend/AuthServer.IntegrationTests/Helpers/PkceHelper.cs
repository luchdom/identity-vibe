using System.Security.Cryptography;
using System.Text;

namespace AuthServer.IntegrationTests.Helpers;

public static class PkceHelper
{
    public static (string CodeVerifier, string CodeChallenge) Create()
    {
        var verifierBytes = RandomNumberGenerator.GetBytes(32);
        var codeVerifier = Base64Url(verifierBytes);

        var challengeBytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        var codeChallenge = Base64Url(challengeBytes);

        return (codeVerifier, codeChallenge);
    }

    private static string Base64Url(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

