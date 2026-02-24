using Microsoft.AspNetCore.WebUtilities;

namespace AuthServer.IntegrationTests.Helpers;

public static class QueryStringHelper
{
    public static string? GetQueryValue(Uri uri, string key)
    {
        var values = QueryHelpers.ParseQuery(uri.Query);
        return values.TryGetValue(key, out var value) ? value.ToString() : null;
    }
}

