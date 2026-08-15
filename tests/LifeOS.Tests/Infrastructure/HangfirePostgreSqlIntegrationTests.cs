using Hangfire.PostgreSql;
using Npgsql;

namespace LifeOS.Tests.Infrastructure;

public sealed class HangfirePostgreSqlIntegrationTests
    : IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;

    public HangfirePostgreSqlIntegrationTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task HangfireStorage_InitializesPrivateSchemaWithoutChangingEfSchema()
    {
        var storage = new PostgreSqlStorage(
            _fixture.ConnectionString,
            new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true
            });

        using (storage.GetConnection())
        {
        }

        await using var connection = new NpgsqlConnection(_fixture.ConnectionString);
        await connection.OpenAsync();

        Assert.True(await SchemaExistsAsync(connection, "hangfire"));
        Assert.True(await TableExistsAsync(connection, "hangfire", "job"));
        Assert.True(await TableExistsAsync(connection, "hangfire", "state"));
        Assert.True(await TableExistsAsync(connection, "hangfire", "server"));
        Assert.True(await TableExistsAsync(connection, "public", "__EFMigrationsHistory"));
        Assert.False(await TableExistsAsync(connection, "public", "job"));
    }

    private static async Task<bool> SchemaExistsAsync(
        NpgsqlConnection connection,
        string schema)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = $1)",
            connection);
        command.Parameters.AddWithValue(schema);
        return (bool)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<bool> TableExistsAsync(
        NpgsqlConnection connection,
        string schema,
        string table)
    {
        await using var command = new NpgsqlCommand(
            "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = $1 AND table_name = $2)",
            connection);
        command.Parameters.AddWithValue(schema);
        command.Parameters.AddWithValue(table);
        return (bool)(await command.ExecuteScalarAsync())!;
    }
}
