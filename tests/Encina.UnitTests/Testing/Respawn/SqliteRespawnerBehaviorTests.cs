using Encina.Testing.Respawn;
using Microsoft.Data.Sqlite;
using EncinaRespawnOptions = Encina.Testing.Respawn.RespawnOptions;

namespace Encina.UnitTests.Testing.Respawn;

/// <summary>
/// Behavior tests for <see cref="SqliteRespawner"/> against a real in-memory SQLite database.
/// </summary>
public sealed class SqliteRespawnerBehaviorTests : IAsyncLifetime
{
    private readonly string _connectionString =
        $"Data Source=respawn-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

    private SqliteConnection _keeper = null!;

    public ValueTask InitializeAsync()
    {
        // A shared in-memory database lives only while one connection stays open.
        _keeper = new SqliteConnection(_connectionString);
        _keeper.Open();
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _keeper.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var command = _keeper.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private async Task<long> CountAsync(string table)
    {
        await using var command = _keeper.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM \"{table}\";";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    private async Task<long> SequenceRowsAsync()
    {
        await using var command = _keeper.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_sequence;";
        return (long)(await command.ExecuteScalarAsync())!;
    }

    [Fact]
    public async Task InitializeAsync_DiscoversUserTablesInNameOrder()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE zebras (Id INTEGER); CREATE TABLE apples (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        await respawner.InitializeAsync();

        // Assert
        respawner.GetDeleteCommands().ShouldBe(
            $"DELETE FROM \"apples\";{Environment.NewLine}DELETE FROM \"zebras\";");
    }

    [Fact]
    public async Task InitializeAsync_CalledTwice_KeepsTheFirstDiscovery()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE first_table (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString);
        await respawner.InitializeAsync();
        await ExecuteAsync("CREATE TABLE second_table (Id INTEGER);");

        // Act
        await respawner.InitializeAsync();

        // Assert
        var commands = respawner.GetDeleteCommands();
        commands.ShouldNotBeNull();
        commands.ShouldNotContain("second_table");
    }

    [Fact]
    public async Task InitializeAsync_WithTablesToIgnore_SkipsThemCaseInsensitively()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE Orders (Id INTEGER); CREATE TABLE MigrationHistory (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString)
        {
            Options = new EncinaRespawnOptions { TablesToIgnore = ["migrationhistory"] }
        };

        // Act
        await respawner.InitializeAsync();

        // Assert
        var commands = respawner.GetDeleteCommands();
        commands.ShouldNotBeNull();
        commands.ShouldContain("Orders");
        commands.ShouldNotContain("MigrationHistory");
    }

    [Fact]
    public async Task InitializeAsync_WhenMessagingTablesAreNotReset_SkipsThem()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE Orders (Id INTEGER); CREATE TABLE OutboxMessages (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString)
        {
            Options = new EncinaRespawnOptions { ResetEncinaMessagingTables = false }
        };

        // Act
        await respawner.InitializeAsync();

        // Assert
        var commands = respawner.GetDeleteCommands();
        commands.ShouldNotBeNull();
        commands.ShouldContain("Orders");
        commands.ShouldNotContain("OutboxMessages");
    }

    [Fact]
    public async Task InitializeAsync_WithDefaultOptions_IncludesMessagingTables()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE OutboxMessages (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        await respawner.InitializeAsync();

        // Assert
        respawner.GetDeleteCommands().ShouldNotBeNull().ShouldContain("OutboxMessages");
    }

    [Fact]
    public async Task InitializeAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        await using var respawner = new SqliteRespawner(_connectionString);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(() => respawner.InitializeAsync(cts.Token));
    }

    [Fact]
    public async Task InitializeAsync_WhenConnectionFails_ReleasesTheLock()
    {
        // Arrange - a read-only connection to a missing file cannot be opened
        var missing = Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.db");
        await using var respawner = new SqliteRespawner($"Data Source={missing};Mode=ReadOnly");

        // Act
        await Should.ThrowAsync<SqliteException>(() => respawner.InitializeAsync());

        // Assert - a second attempt fails the same way instead of blocking on the lock
        await Should.ThrowAsync<SqliteException>(() => respawner.InitializeAsync());
    }

    [Fact]
    public async Task ResetAsync_DeletesAllRowsFromDiscoveredTables()
    {
        // Arrange
        await ExecuteAsync(
            "CREATE TABLE customers (Id INTEGER PRIMARY KEY, Name TEXT);" +
            "CREATE TABLE orders (Id INTEGER PRIMARY KEY, CustomerId INTEGER REFERENCES customers(Id));" +
            "INSERT INTO customers VALUES (1, 'Ada');" +
            "INSERT INTO orders VALUES (1, 1);");
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        await respawner.ResetAsync();

        // Assert
        (await CountAsync("customers")).ShouldBe(0);
        (await CountAsync("orders")).ShouldBe(0);
    }

    [Fact]
    public async Task ResetAsync_LeavesIgnoredTablesUntouched()
    {
        // Arrange
        await ExecuteAsync(
            "CREATE TABLE orders (Id INTEGER);" +
            "CREATE TABLE keep_me (Id INTEGER);" +
            "INSERT INTO orders VALUES (1);" +
            "INSERT INTO keep_me VALUES (1);");
        await using var respawner = new SqliteRespawner(_connectionString)
        {
            Options = new EncinaRespawnOptions { TablesToIgnore = ["keep_me"] }
        };

        // Act
        await respawner.ResetAsync();

        // Assert
        (await CountAsync("orders")).ShouldBe(0);
        (await CountAsync("keep_me")).ShouldBe(1);
    }

    [Fact]
    public async Task ResetAsync_WithReseed_ClearsAutoIncrementSequences()
    {
        // Arrange
        await ExecuteAsync(
            "CREATE TABLE events (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT);" +
            "INSERT INTO events (Name) VALUES ('a'), ('b');");
        (await SequenceRowsAsync()).ShouldBe(1);
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        await respawner.ResetAsync();

        // Assert
        (await SequenceRowsAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task ResetAsync_WithoutReseed_KeepsAutoIncrementSequences()
    {
        // Arrange
        await ExecuteAsync(
            "CREATE TABLE events (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT);" +
            "INSERT INTO events (Name) VALUES ('a'), ('b');");
        await using var respawner = new SqliteRespawner(_connectionString)
        {
            Options = new EncinaRespawnOptions { WithReseed = false }
        };

        // Act
        await respawner.ResetAsync();

        // Assert
        (await CountAsync("events")).ShouldBe(0);
        (await SequenceRowsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task ResetAsync_WithReseedAndNoAutoIncrementTable_DoesNotThrow()
    {
        // Arrange - sqlite_sequence only exists once an AUTOINCREMENT table has been created
        await ExecuteAsync("CREATE TABLE plain (Id INTEGER); INSERT INTO plain VALUES (1);");
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        var exception = await Record.ExceptionAsync(() => respawner.ResetAsync());

        // Assert
        exception.ShouldBeNull();
        (await CountAsync("plain")).ShouldBe(0);
    }

    [Fact]
    public async Task ResetAsync_WhenADiscoveredTableWasDropped_SurfacesTheFailure()
    {
        // Arrange - the table disappears after discovery, so its DELETE fails mid-reset
        await ExecuteAsync("CREATE TABLE gone (Id INTEGER);");
        await using var respawner = new SqliteRespawner(_connectionString);
        await respawner.InitializeAsync();
        await ExecuteAsync("DROP TABLE gone;");

        // Act & Assert
        await Should.ThrowAsync<SqliteException>(() => respawner.ResetAsync());
    }

    [Fact]
    public async Task ResetAsync_CalledTwice_IsIdempotent()
    {
        // Arrange
        await ExecuteAsync("CREATE TABLE orders (Id INTEGER); INSERT INTO orders VALUES (1);");
        await using var respawner = new SqliteRespawner(_connectionString);

        // Act
        await respawner.ResetAsync();
        await ExecuteAsync("INSERT INTO orders VALUES (2);");
        await respawner.ResetAsync();

        // Assert
        (await CountAsync("orders")).ShouldBe(0);
    }
}
