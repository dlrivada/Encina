using System.Data;
using System.Diagnostics.CodeAnalysis;
using Encina.TestInfrastructure.Schemas;
using Microsoft.Data.Sqlite;

namespace Encina.TestInfrastructure.Fixtures;

/// <summary>
/// SQLite database fixture (in-memory, no container needed).
/// Provides a throwaway SQLite instance for integration tests.
/// </summary>
/// <remarks>
/// This fixture uses lazy initialization to ensure the connection is created
/// before it's used, even if xUnit's IAsyncLifetime is not called in time.
/// </remarks>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in DisposeAsync")]
public sealed class SqliteFixture : DatabaseFixture<SqliteConnection>
{
    private SqliteConnection? _connection;
    private readonly object _initLock = new();
    private bool _initialized;
    private readonly string _databaseName = $"EncinaTest_{Guid.NewGuid():N}";

    /// <inheritdoc />
    /// <remarks>
    /// Uses shared cache mode so multiple connections can access the same in-memory database.
    /// A unique database name per fixture instance ensures isolation from parallel tests.
    /// </remarks>
    public override string ConnectionString => $"Data Source={_databaseName};Mode=Memory;Cache=Shared";

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
        await SqliteSchema.CreateTestRepositorySchemaAsync(sqliteConnection);
    }

    /// <inheritdoc />
    protected override Task DropSchemaAsync(IDbConnection connection)
    {
        // For in-memory SQLite, dropping schema is not necessary
        // Connection disposal will destroy the database
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ensures the fixture is initialized (thread-safe, lazy initialization).
    /// </summary>
    private void EnsureInitialized()
    {
        if (_initialized)
            return;

        lock (_initLock)
        {
            if (_initialized)
                return;

            // Perform synchronous initialization
            _connection = new SqliteConnection(ConnectionString);
            _connection.Open();
            Container = _connection;

            // Create schema synchronously
            SqliteSchema.CreateOutboxSchemaAsync(_connection).GetAwaiter().GetResult();
            SqliteSchema.CreateInboxSchemaAsync(_connection).GetAwaiter().GetResult();
            SqliteSchema.CreateSagaSchemaAsync(_connection).GetAwaiter().GetResult();
            SqliteSchema.CreateSchedulingSchemaAsync(_connection).GetAwaiter().GetResult();
            SqliteSchema.CreateTestRepositorySchemaAsync(_connection).GetAwaiter().GetResult();

            _initialized = true;
        }
    }

    /// <inheritdoc />
    public override IDbConnection CreateConnection()
    {
        // Ensure initialization happens (lazy, thread-safe)
        EnsureInitialized();

        // Return the same connection (in-memory SQLite requires keeping connection open)
        return _connection!;
    }

    /// <inheritdoc />
    public override async Task InitializeAsync()
    {
        // If already initialized by lazy init, skip
        if (_initialized)
            return;

        Container = await CreateContainerAsync();

        // Create schema using the same connection
        await CreateSchemaAsync(_connection!);
        _initialized = true;
    }

    /// <inheritdoc />
    public override Task DisposeAsync()
    {
        // Dispose the connection (this destroys the in-memory database)
        _connection?.Dispose();
        _initialized = false;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all data from all tables (but preserves schema).
    /// Use this between tests to ensure clean state.
    /// </summary>
    public async Task ClearAllDataAsync()
    {
        EnsureInitialized();
        await SqliteSchema.ClearAllDataAsync(_connection!);
    }
}
