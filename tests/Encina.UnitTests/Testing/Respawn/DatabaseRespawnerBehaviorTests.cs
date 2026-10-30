using System.Data;
using System.Data.Common;
using Encina.Testing.Respawn;
using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.UnitTests.Testing.Respawn;

/// <summary>
/// Behavior tests for <see cref="DatabaseRespawner"/> driven through a scripted connection:
/// initialization, adapter selection, option translation, reset and disposal.
/// </summary>
public sealed class DatabaseRespawnerBehaviorTests
{
    private sealed class TestRespawner : DatabaseRespawner
    {
        private readonly Func<DbConnection> _factory;
        private readonly RespawnAdapter _adapter;

        public TestRespawner(Func<DbConnection> factory, RespawnAdapter adapter)
        {
            _factory = factory;
            _adapter = adapter;
        }

        protected override string ConnectionString => "scripted";

        protected override RespawnAdapter Adapter => _adapter;

        protected override DbConnection CreateConnection() => _factory();
    }

    private static DataTable Empty(params string[] columns)
    {
        var table = new DataTable();
        foreach (var column in columns)
        {
            table.Columns.Add(column, typeof(string));
        }

        return table;
    }

    private static DataTable ResultFor(string commandText, IReadOnlyList<(string Schema, string Table)> tables)
    {
        var result = Empty("TABLE_SCHEMA", "TABLE_NAME", "a", "b", "c", "d");
        if (commandText.Contains("information_schema.tables", StringComparison.OrdinalIgnoreCase) ||
            commandText.Contains("INFORMATION_SCHEMA.TABLES", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var (schema, table) in tables)
            {
                result.Rows.Add(schema, table, null, null, null, null);
            }
        }

        return result;
    }

    [Fact]
    public async Task InitializeAsync_WhenNoTablesExist_MarksInitializedAndLeavesNoDeleteCommands()
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), executed),
            RespawnAdapter.PostgreSql);

        // Act
        await respawner.InitializeAsync();
        await respawner.ResetAsync();

        // Assert
        respawner.GetDeleteCommands().ShouldBeNull();
        executed.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ResetAsync_WithDiscoveredTables_ExecutesTheGeneratedDeleteCommands()
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, [("public", "orders"), ("public", "customers")]), executed),
            RespawnAdapter.PostgreSql);

        // Act
        await respawner.ResetAsync();

        // Assert
        var deleteSql = respawner.GetDeleteCommands();
        deleteSql.ShouldNotBeNull();
        deleteSql.ShouldContain("orders");
        deleteSql.ShouldContain("customers");
        executed.ShouldContain(deleteSql);
    }

    [Fact]
    public async Task InitializeAsync_WithDefaultOptions_DoesNotExcludeMessagingTables()
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, [("public", "orders")]), executed),
            RespawnAdapter.PostgreSql);

        // Act
        await respawner.InitializeAsync();

        // Assert
        executed.ShouldNotContain(sql => sql.Contains("outboxmessages", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializeAsync_WhenMessagingTablesAreNotReset_IgnoresThem()
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, [("public", "orders"), ("public", "outboxmessages")]), executed),
            RespawnAdapter.PostgreSql)
        {
            Options = new EncinaRespawnOptions { ResetEncinaMessagingTables = false }
        };

        // Act
        await respawner.InitializeAsync();

        // Assert - Respawn filters in SQL, so the exclusion shows in the discovery query
        executed.ShouldContain(sql => sql.Contains("outboxmessages", StringComparison.Ordinal));
        executed.ShouldContain(sql => sql.Contains("sagastates", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializeAsync_WithTablesToIgnore_SkipsThoseTables()
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, [("public", "orders"), ("public", "migrations")]), executed),
            RespawnAdapter.PostgreSql)
        {
            Options = new EncinaRespawnOptions { TablesToIgnore = ["migrations"] }
        };

        // Act
        await respawner.InitializeAsync();

        // Assert - Respawn filters in SQL, so the exclusion shows in the discovery query
        executed.ShouldContain(sql => sql.Contains("migrations", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_DiscoversTablesOnlyOnce()
    {
        // Arrange
        var executed = new List<string>();
        var connections = 0;
        await using var respawner = new TestRespawner(
            () =>
            {
                connections++;
                return new ScriptedDbConnection(sql => ResultFor(sql, []), executed);
            },
            RespawnAdapter.PostgreSql);

        // Act
        await respawner.InitializeAsync();
        await respawner.InitializeAsync();

        // Assert
        connections.ShouldBe(1);
    }

    [Theory]
    [InlineData(RespawnAdapter.SqlServer)]
    [InlineData(RespawnAdapter.PostgreSql)]
    [InlineData(RespawnAdapter.MySql)]
    [InlineData(RespawnAdapter.Oracle)]
    public async Task InitializeAsync_WithSupportedAdapter_QueriesTheDatabase(RespawnAdapter adapter)
    {
        // Arrange
        var executed = new List<string>();
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), executed),
            adapter);

        // Act
        await respawner.InitializeAsync();

        // Assert
        executed.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task InitializeAsync_WithUnknownAdapter_ThrowsNotSupportedException()
    {
        // Arrange
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), []),
            (RespawnAdapter)999);

        // Act & Assert
        await Should.ThrowAsync<NotSupportedException>(() => respawner.InitializeAsync());
    }

    [Fact]
    public async Task InitializeAsync_WhenFailing_ReleasesTheLockSoALaterCallCanRetry()
    {
        // Arrange
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), []),
            (RespawnAdapter)999);

        // Act
        await Should.ThrowAsync<NotSupportedException>(() => respawner.InitializeAsync());

        // Assert - a second attempt fails the same way instead of blocking on the lock
        await Should.ThrowAsync<NotSupportedException>(
            () => respawner.InitializeAsync().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task InitializeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), []),
            RespawnAdapter.PostgreSql);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => respawner.InitializeAsync(cts.Token));
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        // Arrange
        var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), []),
            RespawnAdapter.PostgreSql);

        // Act
        await respawner.DisposeAsync();
        var exception = await Record.ExceptionAsync(async () => await respawner.DisposeAsync());

        // Assert
        exception.ShouldBeNull();
    }

    [Fact]
    public async Task Options_WithSchemaFiltersAndTemporalCheck_AreAcceptedByInitialization()
    {
        // Arrange
        await using var respawner = new TestRespawner(
            () => new ScriptedDbConnection(sql => ResultFor(sql, []), []),
            RespawnAdapter.PostgreSql);
        var options = new EncinaRespawnOptions
        {
            TablesToIgnore = ["keep_me"],
            SchemasToInclude = ["public"],
            SchemasToExclude = ["audit"],
            ResetEncinaMessagingTables = false,
            WithReseed = false,
            CheckTemporalTables = true
        };

        // Act
        respawner.Options = options;

        // Assert
        respawner.Options.ShouldBeSameAs(options);
        await respawner.InitializeAsync();
    }
}
