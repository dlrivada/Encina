using Dapper;
using Microsoft.Data.Sqlite;
using Shouldly;

namespace Encina.Testing.Respawn.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="SqliteRespawner"/>.
/// Uses file-based SQLite for testing since in-memory databases
/// don't persist across connections.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "Sqlite")]
public sealed class SqliteRespawnerIntegrationTests : IAsyncLifetime
{
    private readonly string _dbPath;
    private readonly string _connectionString;

    public SqliteRespawnerIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"respawn_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_dbPath}";
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Create test tables
        await connection.ExecuteAsync("""
            CREATE TABLE OutboxMessages (
                Id TEXT PRIMARY KEY,
                NotificationType TEXT NOT NULL,
                Content TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE InboxMessages (
                MessageId TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                ReceivedAtUtc TEXT NOT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                ExpiresAtUtc TEXT NOT NULL
            );

            CREATE TABLE SagaStates (
                SagaId TEXT PRIMARY KEY,
                SagaType TEXT NOT NULL,
                CurrentStep TEXT NOT NULL,
                Status TEXT NOT NULL,
                Data TEXT NOT NULL,
                StartedAtUtc TEXT NOT NULL,
                LastUpdatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE ScheduledMessages (
                Id TEXT PRIMARY KEY,
                RequestType TEXT NOT NULL,
                Content TEXT NOT NULL,
                ScheduledAtUtc TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0
            );
            """);
    }

    public async Task DisposeAsync()
    {
        // Force SQLite to release the file by clearing the connection pool
        SqliteConnection.ClearAllPools();

        // Retry with exponential backoff to handle file locks under load
        for (int i = 0; i < 5; i++)
        {
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
                return;
            }
            catch (IOException) when (i < 4)
            {
                // Exponential backoff: 50ms, 100ms, 200ms, 400ms
                await Task.Delay(50 * (1 << i));
            }
        }
    }

    [Fact]
    public async Task ResetAsync_WithData_ClearsAllTables()
    {
        // Arrange - Insert test data
        await InsertTestDataAsync();
        var rowsBefore = await CountAllRowsAsync();
        rowsBefore.ShouldBeGreaterThan(0);

        // Create respawner and initialize
        await using var respawner = RespawnerFactory.CreateSqlite(_connectionString);
        await respawner.InitializeAsync();

        // Act
        await respawner.ResetAsync();

        // Assert
        var rowsAfter = await CountAllRowsAsync();
        rowsAfter.ShouldBe(0);
    }

    [Fact]
    public async Task ResetAsync_WithTablesToIgnore_PreservesTables()
    {
        // Arrange - Insert test data
        await InsertTestDataAsync();

        var options = new RespawnOptions
        {
            TablesToIgnore = ["OutboxMessages"]
        };

        await using var respawner = RespawnerFactory.CreateSqlite(_connectionString, options);
        await respawner.InitializeAsync();

        // Act
        await respawner.ResetAsync();

        // Assert - OutboxMessages should be preserved
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var outboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OutboxMessages");
        outboxCount.ShouldBeGreaterThan(0);

        // Other tables should be empty
        var inboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InboxMessages");
        inboxCount.ShouldBe(0);
    }

    [Fact]
    public async Task ResetAsync_WithResetEncinaMessagingTablesFalse_PreservesMessagingTables()
    {
        // Arrange - Insert test data
        await InsertTestDataAsync();

        var options = new RespawnOptions
        {
            ResetEncinaMessagingTables = false
        };

        await using var respawner = RespawnerFactory.CreateSqlite(_connectionString, options);
        await respawner.InitializeAsync();

        // Act
        await respawner.ResetAsync();

        // Assert - All messaging tables should be preserved
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var outboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OutboxMessages");
        outboxCount.ShouldBeGreaterThan(0);

        var inboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InboxMessages");
        inboxCount.ShouldBeGreaterThan(0);

        var sagaCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SagaStates");
        sagaCount.ShouldBeGreaterThan(0);

        var scheduledCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ScheduledMessages");
        scheduledCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ResetAsync_CalledMultipleTimes_WorksCorrectly()
    {
        // Arrange
        await using var respawner = RespawnerFactory.CreateSqlite(_connectionString);
        await respawner.InitializeAsync();

        // Act & Assert - Multiple resets should work
        for (int i = 0; i < 3; i++)
        {
            await InsertTestDataAsync();
            var rowsBefore = await CountAllRowsAsync();
            rowsBefore.ShouldBeGreaterThan(0);

            await respawner.ResetAsync();

            var rowsAfter = await CountAllRowsAsync();
            rowsAfter.ShouldBe(0);
        }
    }

    [Fact]
    public async Task GetDeleteCommands_AfterInitialization_ReturnsCommands()
    {
        // Arrange
        await using var respawner = RespawnerFactory.CreateSqlite(_connectionString);
        await respawner.InitializeAsync();

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldNotBeNull();
        commands.ShouldNotBeEmpty();
        // SQLite respawner should return DELETE statements for each table
        commands.ShouldContain("DELETE FROM");
    }

    private async Task InsertTestDataAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        // Insert OutboxMessage
        await connection.ExecuteAsync("""
            INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount)
            VALUES (@Id, @NotificationType, @Content, @CreatedAtUtc, @RetryCount)
            """,
            new
            {
                Id = Guid.NewGuid().ToString(),
                NotificationType = "TestNotification",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                RetryCount = 0
            });

        // Insert InboxMessage
        await connection.ExecuteAsync("""
            INSERT INTO InboxMessages (MessageId, RequestType, ReceivedAtUtc, RetryCount, ExpiresAtUtc)
            VALUES (@MessageId, @RequestType, @ReceivedAtUtc, @RetryCount, @ExpiresAtUtc)
            """,
            new
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow.ToString("O"),
                RetryCount = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1).ToString("O")
            });

        // Insert SagaState
        await connection.ExecuteAsync("""
            INSERT INTO SagaStates (SagaId, SagaType, CurrentStep, Status, Data, StartedAtUtc, LastUpdatedAtUtc)
            VALUES (@SagaId, @SagaType, @CurrentStep, @Status, @Data, @StartedAtUtc, @LastUpdatedAtUtc)
            """,
            new
            {
                SagaId = Guid.NewGuid().ToString(),
                SagaType = "TestSaga",
                CurrentStep = "Step1",
                Status = "InProgress",
                Data = "{}",
                StartedAtUtc = DateTime.UtcNow.ToString("O"),
                LastUpdatedAtUtc = DateTime.UtcNow.ToString("O")
            });

        // Insert ScheduledMessage
        await connection.ExecuteAsync("""
            INSERT INTO ScheduledMessages (Id, RequestType, Content, ScheduledAtUtc, CreatedAtUtc, RetryCount)
            VALUES (@Id, @RequestType, @Content, @ScheduledAtUtc, @CreatedAtUtc, @RetryCount)
            """,
            new
            {
                Id = Guid.NewGuid().ToString(),
                RequestType = "TestScheduledRequest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(1).ToString("O"),
                CreatedAtUtc = DateTime.UtcNow.ToString("O"),
                RetryCount = 0
            });
    }

    private async Task<int> CountAllRowsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var total = 0;
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OutboxMessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InboxMessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SagaStates");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ScheduledMessages");

        return total;
    }
}
