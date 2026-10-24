using Npgsql;
using Testcontainers.PostgreSql;

namespace Encina.IntegrationTests.Cdc.PostgreSql;

/// <summary>
/// Shared PostgreSQL container started with <c>wal_level=logical</c> so the logical-replication
/// connector can create publications and replication slots. The standard
/// <c>PostgreSqlFixture</c> keeps the server default (<c>replica</c>), which cannot host a
/// pgoutput slot, so CDC tests own this collection fixture.
/// </summary>
public sealed class PostgreSqlCdcFixture : IAsyncLifetime
{
    /// <summary>The replication slot the connector always creates (hard-coded in the connector).</summary>
    public const string SlotName = "encina_cdc_slot";

    private PostgreSqlContainer? _container;

    /// <summary>Gets the connection string of the logical-replication-enabled database.</summary>
    public string ConnectionString => _container?.GetConnectionString() ?? string.Empty;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("encina_cdc_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCommand(
                "-c", "wal_level=logical",
                "-c", "max_replication_slots=20",
                "-c", "max_wal_senders=20")
            .WithCleanUp(true)
            .Build();

        await _container.StartAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Resets the database to a known state: drops the replication slot and the publication,
    /// then recreates the <c>cdc_orders</c> table (<c>REPLICA IDENTITY FULL</c>) empty.
    /// </summary>
    public async Task ResetAsync(string publicationName, CancellationToken cancellationToken = default)
    {
        await DropSlotAsync(cancellationToken);

        await ExecuteAsync($"DROP PUBLICATION IF EXISTS {publicationName}", cancellationToken);
        await ExecuteAsync("DROP TABLE IF EXISTS public.cdc_orders", cancellationToken);
        await ExecuteAsync(
            "CREATE TABLE public.cdc_orders (id integer PRIMARY KEY, name text, note text)",
            cancellationToken);
        await ExecuteAsync("ALTER TABLE public.cdc_orders REPLICA IDENTITY FULL", cancellationToken);

        // Default replica identity: updates and deletes carry only the primary key as old values.
        await ExecuteAsync("DROP TABLE IF EXISTS public.cdc_plain", cancellationToken);
        await ExecuteAsync(
            "CREATE TABLE public.cdc_plain (id integer PRIMARY KEY, name text)",
            cancellationToken);
    }

    /// <summary>Creates the logical replication slot ahead of the connector.</summary>
    public Task CreateSlotAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            $"SELECT pg_create_logical_replication_slot('{SlotName}', 'pgoutput')",
            cancellationToken);

    /// <summary>Executes a non-query statement on a fresh connection.</summary>
    public async Task ExecuteAsync(string sql, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>Returns whether a walsender is currently attached to the slot.</summary>
    public async Task<bool> IsSlotActiveAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COALESCE(bool_or(active), false) FROM pg_replication_slots WHERE slot_name = '{SlotName}'";
        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task DropSlotAsync(CancellationToken cancellationToken)
    {
        // A previous test's walsender may take a moment to release the slot after its
        // connection was disposed, so retry the drop while the slot is still active.
        for (var attempt = 0; attempt < 50; attempt++)
        {
            try
            {
                await ExecuteAsync(
                    $"SELECT pg_drop_replication_slot(slot_name) FROM pg_replication_slots WHERE slot_name = '{SlotName}'",
                    cancellationToken);
                return;
            }
            catch (PostgresException ex) when (ex.SqlState == "55006")
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        throw new InvalidOperationException($"Replication slot '{SlotName}' stayed active.");
    }
}
