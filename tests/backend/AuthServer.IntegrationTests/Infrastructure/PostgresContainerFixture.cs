using Npgsql;
using Testcontainers.PostgreSql;

namespace AuthServer.IntegrationTests.Infrastructure;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:15-alpine")
        .WithDatabase("postgres")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string AdminConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public string BuildConnectionString(string databaseName)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = databaseName
        };

        return builder.ConnectionString;
    }

    public async Task EnsureDatabaseExistsAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var adminBuilder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = "postgres"
        };

        await using var connection = new NpgsqlConnection(adminBuilder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var existsCommand = connection.CreateCommand())
        {
            existsCommand.CommandText = "SELECT 1 FROM pg_database WHERE datname = @db";
            existsCommand.Parameters.AddWithValue("db", databaseName);

            var exists = await existsCommand.ExecuteScalarAsync(cancellationToken);
            if (exists is not null)
            {
                return;
            }
        }

        var escapedDatabaseName = databaseName.Replace("\"", "\"\"");
        await using var createCommand = connection.CreateCommand();
        createCommand.CommandText = $"CREATE DATABASE \"{escapedDatabaseName}\"";
        await createCommand.ExecuteNonQueryAsync(cancellationToken);
    }
}
