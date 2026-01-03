using Dapper;
using Encina.TestInfrastructure.Fixtures;
using Npgsql;
using Shouldly;

namespace Encina.Testing.Respawn.IntegrationTests;

/// <summary>
/// Integration tests for <see cref="PostgreSqlRespawner"/>.
/// Tests actual database reset operations against a real PostgreSQL instance.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "PostgreSql")]
public sealed class PostgreSqlRespawnerIntegrationTests : IClassFixture<PostgreSqlFixture>, IAsyncLifetime, IDisposable
{
    private readonly PostgreSqlFixture _fixture;
    private PostgreSqlRespawner _respawner = null!;

    public PostgreSqlRespawnerIntegrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _respawner = new PostgreSqlRespawner(_fixture.ConnectionString);
        await _respawner.InitializeAsync();
        await _respawner.ResetAsync();
    }

    public async Task DisposeAsync()
    {
        await _respawner.DisposeAsync();
    }

    public void Dispose()
    {
        // Respawner only implements IAsyncDisposable, disposal handled in DisposeAsync
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ResetAsync_WithData_ClearsAllTables()
    {
        // Arrange - Insert test data
        await InsertTestDataAsync();
        var rowsBefore = await CountAllRowsAsync();
        rowsBefore.ShouldBeGreaterThan(0);

        // Act - Use class-level respawner
        await _respawner.ResetAsync();

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
            TablesToIgnore = ["outboxmessages"] // PostgreSQL uses lowercase
        };

        await using var respawner = new PostgreSqlRespawner(_fixture.ConnectionString);
        respawner.Options = options;
        await respawner.InitializeAsync();

        // Act
        await respawner.ResetAsync();

        // Assert - outboxmessages should be preserved
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();
        var outboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM outboxmessages");
        outboxCount.ShouldBeGreaterThan(0);

        // Other tables should be empty
        var inboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM inboxmessages");
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

        await using var respawner = new PostgreSqlRespawner(_fixture.ConnectionString);
        respawner.Options = options;
        await respawner.InitializeAsync();

        // Act
        await respawner.ResetAsync();

        // Assert - All messaging tables should be preserved
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();

        var outboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM outboxmessages");
        outboxCount.ShouldBeGreaterThan(0);

        var inboxCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM inboxmessages");
        inboxCount.ShouldBeGreaterThan(0);

        var sagaCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sagastates");
        sagaCount.ShouldBeGreaterThan(0);

        var scheduledCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM scheduledmessages");
        scheduledCount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task ResetAsync_CalledMultipleTimes_WorksCorrectly()
    {
        // Arrange
        await using var respawner = new PostgreSqlRespawner(_fixture.ConnectionString);
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
        await using var respawner = new PostgreSqlRespawner(_fixture.ConnectionString);
        await respawner.InitializeAsync();

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldNotBeNull();
        commands.ShouldNotBeEmpty();
    }

    private async Task InsertTestDataAsync()
    {
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();

        // Insert OutboxMessage
        await connection.ExecuteAsync("""
            INSERT INTO outboxmessages (id, notificationtype, content, createdatutc, retrycount)
            VALUES (@Id, @NotificationType, @Content, @CreatedAtUtc, @RetryCount)
            """,
            new
            {
                Id = Guid.NewGuid(),
                NotificationType = "TestNotification",
                Content = "{}",
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            });

        // Insert InboxMessage
        await connection.ExecuteAsync("""
            INSERT INTO inboxmessages (messageid, requesttype, receivedatutc, retrycount, expiresatutc)
            VALUES (@MessageId, @RequestType, @ReceivedAtUtc, @RetryCount, @ExpiresAtUtc)
            """,
            new
            {
                MessageId = Guid.NewGuid().ToString(),
                RequestType = "TestRequest",
                ReceivedAtUtc = DateTime.UtcNow,
                RetryCount = 0,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
            });

        // Insert SagaState
        await connection.ExecuteAsync("""
            INSERT INTO sagastates (sagaid, sagatype, currentstep, status, data, startedatutc, lastupdatedatutc)
            VALUES (@SagaId, @SagaType, @CurrentStep, @Status, @Data, @StartedAtUtc, @LastUpdatedAtUtc)
            """,
            new
            {
                SagaId = Guid.NewGuid(),
                SagaType = "TestSaga",
                CurrentStep = "Step1",
                Status = "InProgress",
                Data = "{}",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            });

        // Insert ScheduledMessage
        await connection.ExecuteAsync("""
            INSERT INTO scheduledmessages (id, requesttype, content, scheduledatutc, retrycount)
            VALUES (@Id, @RequestType, @Content, @ScheduledAtUtc, @RetryCount)
            """,
            new
            {
                Id = Guid.NewGuid(),
                RequestType = "TestScheduledRequest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
                RetryCount = 0
            });
    }

    private async Task<int> CountAllRowsAsync()
    {
        using var connection = (NpgsqlConnection)_fixture.CreateConnection();

        var total = 0;
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM outboxmessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM inboxmessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sagastates");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM scheduledmessages");

        return total;
    }
}
