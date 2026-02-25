using System.Security.Claims;
using FluentAssertions;
using Shared.Extensions;
using Xunit;

namespace Shared.UnitTests.Extensions;

public sealed class ClaimsExtensionsTests
{
    [Fact]
    public void GetScopes_SplitsSpaceSeparatedScopeClaim_AndDeduplicates()
    {
        var principal = CreatePrincipal(
            new Claim("scope", "orders.read orders.write"),
            new Claim("scope", "orders.read"));

        var scopes = principal.GetScopes().ToArray();

        scopes.Should().BeEquivalentTo(["orders.read", "orders.write"]);
    }

    [Fact]
    public void HasScope_ReturnsTrue_ForExactScopeInSpaceSeparatedClaim()
    {
        var principal = CreatePrincipal(new Claim("scope", "orders.read orders.write"));

        principal.HasScope("orders.read").Should().BeTrue();
        principal.HasScope("orders.write").Should().BeTrue();
    }

    [Fact]
    public void HasScope_DoesNotMatchPartialScopeNames()
    {
        var principal = CreatePrincipal(new Claim("scope", "orders.read.all"));

        principal.HasScope("orders.read").Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_ForOrdersManageScope()
    {
        var principal = CreatePrincipal(new Claim("scope", "orders.manage"));

        principal.IsAdmin().Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_ForAdminFullScope()
    {
        var principal = CreatePrincipal(new Claim("scope", "admin.full"));

        principal.IsAdmin().Should().BeTrue();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        return new ClaimsPrincipal(identity);
    }
}
