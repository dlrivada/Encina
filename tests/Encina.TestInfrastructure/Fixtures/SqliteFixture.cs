using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Encina.TestInfrastructure.Schemas;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// SQLite database fixture (in-memory, no container needed).
/// Provides a throwaway SQLite instance for integration tests.
/// </summary>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in DisposeAsync")]
public sealed class SqliteFixture : DatabaseFixture<SqliteConnection>
{
    private SqliteConnection? _connection;

    /// <inheritdoc />
    public override string ConnectionString => "Data Source=:memory:";

    /// <inheritdoc />
    public override string ProviderName => "SQLite";

    /// <inheritdoc />
    protected override async Task<SqliteConnection> CreateContainerAsync()
    {
        // SQLite doesn't need a container, just create in-memory connection
        _connection = new SqliteConnection(ConnectionString);
        _connection.Open();

        return await Task.FromResult(_connection);
    }

    /// <inheritdoc />
    protected override async Task CreateSchemaAsync(IDbConnection connection)
    {
        if (connection is not SqliteConnection sqliteConnection)
        {
            throw new InvalidOperationException("Connection must be SqliteConnection");
        }

        await SqliteSchema.CreateOutboxSchemaAsync(sqliteConnection);
        await SqliteSchema.CreateInboxSchemaAsync(sqliteConnection);
        await SqliteSchema.CreateSagaSchemaAsync(sqliteConnection);
        await SqliteSchema.CreateSchedulingSchemaAsync(sqliteConnection);
    }

    /// <inheritdoc />
    protected override Task DropSchemaAsync(IDbConnection connection)
    {
        // For in-memory SQLite, dropping schema is not necessary
        // Connection disposal will destroy the database
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        // Return the same connection (in-memory SQLite requires keeping connection open)
        if (_connection is null || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("SQLite connection not initialized");
        }

        return _connection;
    }

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        Container = await CreateContainerAsync();

        // Create schema using the same connection
        await CreateSchemaAsync(_connection!);
    }

    /// <inheritdoc />
    public override Task DisposeAsync()
    {
        // Dispose the connection (this destroys the in-memory database)
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        if (_connection is null || _connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("SQLite connection not initialized");
        }

        await SqliteSchema.ClearAllDataAsync(_connection);
    }
}
