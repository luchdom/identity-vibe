using Xunit;

namespace AuthServer.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class AuthServerIntegrationTestCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "AuthServer Integration Collection";
}

