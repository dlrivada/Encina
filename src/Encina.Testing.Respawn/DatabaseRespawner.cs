using System.Data.Common;
using Respawn;

namespace Encina.Testing.Respawn;

/// <summary>
/// Abstract base class for database-specific Respawn implementations.
/// Provides intelligent database cleanup between integration tests.
/// </summary>
/// <remarks>
/// <para>
/// Respawn analyzes foreign key relationships to delete data in the correct order,
/// providing fast and reliable database reset without recreating the schema.
/// </para>
/// <para>
/// This is more efficient than:
/// - Creating new containers per test (slow)
/// - Manual DELETE statements (error-prone, FK order issues)
/// - Transaction rollback (not realistic for some scenarios)
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OrderTests : IClassFixture&lt;SqlServerRespawner&gt;, IAsyncLifetime
/// {
///     private readonly SqlServerRespawner _respawner;
///
///     public OrderTests(SqlServerRespawner respawner) => _respawner = respawner;
///
///     public Task InitializeAsync() => Task.CompletedTask;
///
///     public Task DisposeAsync() => _respawner.ResetAsync();
///
///     [Fact]
///     public async Task CreateOrder_ShouldPersist()
///     {
///         // Test runs with a clean database
///     }
/// }
/// </code>
/// </example>
public abstract class DatabaseRespawner : IAsyncDisposable
{
    private Respawner? _respawner;
    private bool _isInitialized;
    private int _disposed;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    /// <summary>
    /// Gets the connection string for the database.
    /// </summary>
    protected abstract string ConnectionString { get; }

    /// <summary>
    /// Gets the database adapter type for Respawn.
    /// </summary>
    protected abstract RespawnAdapter Adapter { get; }

    /// <summary>
    /// Gets or sets the configuration options for Respawn.
    /// </summary>
    public RespawnOptions Options { get; set; } = new();

    /// <summary>
    /// Creates a new database connection.
    /// </summary>
    /// <returns>An open database connection.</returns>
    protected abstract DbConnection CreateConnection();

    /// <summary>
    /// Initializes the Respawner with the configured options.
    /// This method is called automatically on first reset, but can be called
    /// explicitly for eager initialization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        await _initializationLock.WaitAsync(cancellationToken);
        try
        {
            if (_isInitialized)
            {
                return;
            }

            await using var connection = CreateConnection();
            await connection.OpenAsync(cancellationToken);

            var respawnerOptions = BuildRespawnerOptions();
            _respawner = await Respawner.CreateAsync(connection, respawnerOptions);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Resets the database to a clean state by deleting all data from tables
    /// (respecting foreign key constraints).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await _respawner!.ResetAsync(connection);
    }

    /// <summary>
    /// Gets the DELETE commands that would be executed on reset.
    /// Useful for debugging and understanding the cleanup order.
    /// </summary>
    /// <returns>The generated DELETE SQL statements, or null if not initialized.</returns>
    public string? GetDeleteCommands()
    {
        return _respawner?.DeleteSql;
    }

    /// <summary>
    /// Builds the Respawner options from the configured RespawnOptions.
    /// </summary>
    private RespawnerOptions BuildRespawnerOptions()
    {
        var tablesToIgnore = Options.TablesToIgnore.ToList();

        // If not resetting Encina tables, add them to ignore list
        if (!Options.ResetEncinaMessagingTables)
        {
            tablesToIgnore.AddRange(RespawnOptions.EncinaMessagingTables);
        }

        return new RespawnerOptions
        {
            DbAdapter = GetDbAdapter(),
            TablesToIgnore = [.. tablesToIgnore],
            WithReseed = Options.WithReseed,
            CheckTemporalTables = Options.CheckTemporalTables,
            SchemasToInclude = Options.SchemasToInclude,
            SchemasToExclude = Options.SchemasToExclude
        };
    }

    /// <summary>
    /// Gets the Respawn database adapter for this provider.
    /// </summary>
    private IDbAdapter GetDbAdapter()
    {
        return Adapter switch
        {
            RespawnAdapter.SqlServer => DbAdapter.SqlServer,
            RespawnAdapter.PostgreSql => DbAdapter.Postgres,
            RespawnAdapter.MySql => DbAdapter.MySql,
            RespawnAdapter.Oracle => DbAdapter.Oracle,
            _ => throw new NotSupportedException($"Database adapter {Adapter} is not supported by Respawn")
        };
    }

    /// <summary>
    /// Disposes of resources used by the respawner.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return ValueTask.CompletedTask;
        }

        _initializationLock.Dispose();

        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Supported database adapters for Respawn.
/// </summary>
public enum RespawnAdapter
{
    /// <summary>SQL Server database.</summary>
    SqlServer,

    /// <summary>PostgreSQL database.</summary>
    PostgreSql,

    /// <summary>MySQL database.</summary>
    MySql,

    /// <summary>Oracle database.</summary>
    Oracle
}
