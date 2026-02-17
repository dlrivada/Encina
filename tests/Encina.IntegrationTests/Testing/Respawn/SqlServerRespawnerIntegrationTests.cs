using Dapper;
using Encina.TestInfrastructure.Fixtures;
using Microsoft.Data.SqlClient;
using Shouldly;

namespace Encina.IntegrationTests.Testing.Respawn;

/// <summary>
/// Integration tests for <see cref="SqlServerRespawner"/>.
/// Tests actual database reset operations against a real SQL Server instance.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Provider", "SqlServer")]
[Collection("ADO-SqlServer")]
public sealed class SqlServerRespawnerIntegrationTests : IAsyncLifetime, IDisposable
{
    private readonly SqlServerFixture _fixture;
    private SqlServerRespawner _respawner = null!;

    public SqlServerRespawnerIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        _respawner = new SqlServerRespawner(_fixture.ConnectionString);
        await _respawner.InitializeAsync();
        await _respawner.ResetAsync();
    }

    public async ValueTask DisposeAsync()
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
        var options = new RespawnOptions
        {
            TablesToIgnore = ["OutboxMessages"]
        };

        await using var respawner = new SqlServerRespawner(_fixture.ConnectionString);
        respawner.Options = options;
        await respawner.InitializeAsync();
        await InsertTestDataAsync(respawner);

        // Act
        await respawner.ResetAsync();

        // Assert - OutboxMessages should be preserved
        using var connection = (SqlConnection)_fixture.CreateConnection();
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
        var options = new RespawnOptions
        {
            ResetEncinaMessagingTables = false
        };

        await using var respawner = new SqlServerRespawner(_fixture.ConnectionString);
        respawner.Options = options;
        await respawner.InitializeAsync();
        await InsertTestDataAsync(respawner);

        // Act
        await respawner.ResetAsync();

        // Assert - All messaging tables should be preserved
        using var connection = (SqlConnection)_fixture.CreateConnection();

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
        await using var respawner = new SqlServerRespawner(_fixture.ConnectionString);
        await respawner.InitializeAsync();

        // Act & Assert - Multiple resets should work
        for (int i = 0; i < 3; i++)
        {
            await InsertTestDataAsync(respawner);
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
        await using var respawner = new SqlServerRespawner(_fixture.ConnectionString);
        await respawner.InitializeAsync();

        // Act
        var commands = respawner.GetDeleteCommands();

        // Assert
        commands.ShouldNotBeNull();
        commands.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ResetAsync_WithForeignKeyRelationships_DeletesInCorrectOrder()
    {
        // This test verifies that Respawn handles FK relationships correctly
        // The Encina schema has no FK relationships between messaging tables,
        // but we verify the reset works without FK violations

        // Arrange
        await using var respawner = new SqlServerRespawner(_fixture.ConnectionString);
        await respawner.InitializeAsync();
        await InsertTestDataAsync(respawner);

        // Act - Should not throw FK constraint violations
        await respawner.ResetAsync();

        // Assert
        var rowsAfter = await CountAllRowsAsync();
        rowsAfter.ShouldBe(0);
    }

    private async Task InsertTestDataAsync(SqlServerRespawner? respawner = null)
    {
        // Ensure clean state before inserting test data
        await (respawner ?? _respawner).ResetAsync();

        using var connection = (SqlConnection)_fixture.CreateConnection();

        // Insert OutboxMessage
        await connection.ExecuteAsync("""
            INSERT INTO OutboxMessages (Id, NotificationType, Content, CreatedAtUtc, RetryCount)
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
            INSERT INTO InboxMessages (MessageId, RequestType, ReceivedAtUtc, RetryCount, ExpiresAtUtc)
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
            INSERT INTO SagaStates (SagaId, SagaType, CurrentStep, Status, Data, StartedAtUtc, LastUpdatedAtUtc)
            VALUES (@SagaId, @SagaType, @CurrentStep, @Status, @Data, @StartedAtUtc, @LastUpdatedAtUtc)
            """,
            new
            {
                SagaId = Guid.NewGuid(),
                SagaType = "TestSaga",
                CurrentStep = 1,
                Status = "InProgress",
                Data = "{}",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            });

        // Insert ScheduledMessage
        await connection.ExecuteAsync("""
            INSERT INTO ScheduledMessages (Id, RequestType, Content, ScheduledAtUtc, RetryCount, CreatedAtUtc)
            VALUES (@Id, @RequestType, @Content, @ScheduledAtUtc, @RetryCount, @CreatedAtUtc)
            """,
            new
            {
                Id = Guid.NewGuid(),
                RequestType = "TestScheduledRequest",
                Content = "{}",
                ScheduledAtUtc = DateTime.UtcNow.AddHours(1),
                RetryCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            });
    }

    private async Task<int> CountAllRowsAsync()
    {
        using var connection = (SqlConnection)_fixture.CreateConnection();

        var total = 0;
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OutboxMessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InboxMessages");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SagaStates");
        total += await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ScheduledMessages");

        return total;
    }
}
