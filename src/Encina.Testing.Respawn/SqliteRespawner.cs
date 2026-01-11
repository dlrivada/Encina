using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace Encina.Testing.Respawn;

/// <summary>
/// Respawn-like implementation for SQLite databases.
/// </summary>
/// <remarks>
/// <para>
/// SQLite is not officially supported by Respawn, so this implementation
/// provides a compatible interface using manual DELETE statements.
/// </para>
/// <para>
/// For in-memory SQLite databases, consider recreating the database
/// instead of using Respawn, as it may be faster.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class SqliteTests : IClassFixture&lt;SqliteRespawner&gt;, IAsyncLifetime
/// {
///     private readonly SqliteRespawner _respawner;
///
///     public SqliteTests(SqliteRespawner respawner)
///     {
///         _respawner = respawner;
///     }
///
///     public Task InitializeAsync() => _respawner.InitializeAsync();
///     public Task DisposeAsync() => _respawner.ResetAsync();
/// }
/// </code>
/// </example>
public sealed class SqliteRespawner : IAsyncDisposable
{
    private readonly string _connectionString;
    private List<string>? _tables;
    private bool _isInitialized;
    private int _disposed;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteRespawner"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string.</param>
    public SqliteRespawner(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// Gets or sets the configuration options for Respawn.
    /// </summary>
    public RespawnOptions Options { get; set; } = new();

    /// <summary>
    /// Initializes the respawner by discovering tables in the database.
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

            await using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            _tables = await DiscoverTablesAsync(connection, cancellationToken);
            _isInitialized = true;
        }
        finally
        {
            _initializationLock.Release();
        }
    }

    /// <summary>
    /// Resets the database by deleting all data from discovered tables.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Disable foreign key checks temporarily
        await using (var pragmaCommand = connection.CreateCommand())
        {
            pragmaCommand.CommandText = "PRAGMA foreign_keys = OFF;";
            await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        try
        {
            foreach (var table in _tables!)
            {
                await using var deleteCommand = connection.CreateCommand();
                // Table names come from sqlite_master system table, not from user input,
                // so SQL injection is not possible here. The names are already validated
                // identifiers that exist in the database schema.
                deleteCommand.CommandText = $"DELETE FROM \"{table}\";";
                await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            // Reset auto-increment sequences if WithReseed is true
            if (Options.WithReseed)
            {
                await using var resetCommand = connection.CreateCommand();
                resetCommand.CommandText = "DELETE FROM sqlite_sequence;";
                try
                {
                    await resetCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("no such table: sqlite_sequence", StringComparison.OrdinalIgnoreCase))
                {
                    // sqlite_sequence may not exist if no AUTOINCREMENT columns are used
                }
            }
        }
        finally
        {
            // Re-enable foreign key checks
            await using var pragmaCommand = connection.CreateCommand();
            pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
            await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Gets the DELETE commands that would be executed on reset.
    /// </summary>
    /// <returns>The generated DELETE SQL statements, or null if not initialized.</returns>
    public string? GetDeleteCommands()
    {
        if (_tables is null)
        {
            return null;
        }

        return string.Join(Environment.NewLine,
            _tables.Select(t => $"DELETE FROM \"{t}\";"));
    }

    /// <summary>
    /// Discovers all user tables in the database.
    /// </summary>
    private async Task<List<string>> DiscoverTablesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var tables = new List<string>();
        var tablesToIgnore = new HashSet<string>(Options.TablesToIgnore, StringComparer.OrdinalIgnoreCase);

        // Add Encina tables to ignore list if not resetting them
        if (!Options.ResetEncinaMessagingTables)
        {
            foreach (var table in RespawnOptions.EncinaMessagingTables)
            {
                tablesToIgnore.Add(table);
            }
        }

        await using var command = connection.CreateCommand();
        command.CommandText = @"
            SELECT name FROM sqlite_master
            WHERE type = 'table'
            AND name NOT LIKE 'sqlite_%'
            ORDER BY name;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var tableName = reader.GetString(0);
            if (!tablesToIgnore.Contains(tableName))
            {
                tables.Add(tableName);
            }
        }

        return tables;
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
