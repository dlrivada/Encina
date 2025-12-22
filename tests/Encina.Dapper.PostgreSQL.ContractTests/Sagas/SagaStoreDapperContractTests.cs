using Encina.Dapper.PostgreSQL.Sagas;
using Encina.Messaging.Sagas;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.PostgreSQL.Tests.Sagas;

/// <summary>
/// Contract tests for <see cref="SagaStoreDapper"/>.
/// Verifies compliance with <see cref="ISagaStore"/> interface contract.
/// Uses real PostgreSQL database via Testcontainers.
/// </summary>
[Trait("Category", "Contract")]
public sealed class SagaStoreDapperContractTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;
    private readonly SagaStoreDapper _store;

    public SagaStoreDapperContractTests(PostgreSqlFixture database)
    {
        _database = database;

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new SagaStoreDapper(_database.CreateConnection());
    }

    #region Contract: GetAsync

    [Fact]
    public async Task GetAsync_Contract_ReturnsISagaState()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act
        var result = await _store.GetAsync(sagaId);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<ISagaState>(result);
    }

    [Fact]
    public async Task GetAsync_Contract_ReturnsNullForNonExistent()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _store.GetAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        var result = await _store.GetAsync(sagaId, cts.Token);
        Assert.Null(result);
    }

    #endregion

    #region Contract: AddAsync

    [Fact]
    public async Task AddAsync_Contract_AcceptsISagaState()
    {
        // Arrange
        ISagaState saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ContractSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };

        // Act & Assert - Should not throw
        await _store.AddAsync(saga);
    }

    [Fact]
    public async Task AddAsync_Contract_PersistsAllRequiredFields()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "FieldTestSaga",
            Data = "{\"test\":true}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 2
        };

        // Act
        await _store.AddAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(sagaId, retrieved.SagaId);
        Assert.Equal("FieldTestSaga", retrieved.SagaType);
        Assert.Equal("{\"test\":true}", retrieved.Data);
        Assert.Equal("Running", retrieved.Status);
        Assert.Equal(2, retrieved.CurrentStep);
    }

    [Fact]
    public async Task AddAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CancellableSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.AddAsync(saga, cts.Token);
    }

    #endregion

    #region Contract: UpdateAsync

    [Fact]
    public async Task UpdateAsync_Contract_AcceptsISagaState()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
#pragma warning disable CA1859 // Use concrete types when possible for improved performance - Intentionally testing interface contract
        ISagaState saga = new SagaState
#pragma warning restore CA1859
        {
            SagaId = sagaId,
            SagaType = "UpdateContractSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act
        saga.Status = "Completed";
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("Completed", retrieved.Status);
    }

    [Fact]
    public async Task UpdateAsync_Contract_UpdatesAllFields()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "FieldUpdateSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act
        saga.SagaType = "UpdatedType";
        saga.Data = "{\"updated\":true}";
        saga.Status = "Completed";
        saga.CurrentStep = 5;
        saga.CompletedAtUtc = DateTime.UtcNow;
        saga.ErrorMessage = "Test error";
        await _store.UpdateAsync(saga);

        // Assert
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal("UpdatedType", retrieved.SagaType);
        Assert.Equal("{\"updated\":true}", retrieved.Data);
        Assert.Equal("Completed", retrieved.Status);
        Assert.Equal(5, retrieved.CurrentStep);
        Assert.NotNull(retrieved.CompletedAtUtc);
        Assert.Equal("Test error", retrieved.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsync_Contract_SupportsCancellation()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "CancellableSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        saga.Status = "Completed";
        await _store.UpdateAsync(saga, cts.Token);
    }

    #endregion

    #region Contract: GetStuckSagasAsync

    [Fact]
    public async Task GetStuckSagasAsync_Contract_ReturnsIEnumerableOfISagaState()
    {
        // Arrange
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEnumerable<ISagaState>>(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetStuckSagasAsync_Contract_FiltersByRunning()
    {
        // Arrange
        var runningSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RunningSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 1
        };
        await _store.AddAsync(runningSaga);

        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = "Completed",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-4),
            CurrentStep = 3
        };
        await _store.AddAsync(completedSaga);

        // Act
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = result.ToList();
        Assert.Single(stuckList);
        Assert.Equal("Running", stuckList[0].Status);
    }

    [Fact]
    public async Task GetStuckSagasAsync_Contract_FiltersByCompensating()
    {
        // Arrange
        var compensatingSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompensatingSaga",
            Data = "{}",
            Status = "Compensating",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 2
        };
        await _store.AddAsync(compensatingSaga);

        // Act
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = result.ToList();
        Assert.Single(stuckList);
        Assert.Equal("Compensating", stuckList[0].Status);
    }

    [Fact]
    public async Task GetStuckSagasAsync_Contract_RespectsOlderThan()
    {
        // Arrange
        var oldSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OldSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 1
        };
        await _store.AddAsync(oldSaga);

        var recentSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "RecentSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CurrentStep = 1
        };
        await _store.AddAsync(recentSaga);

        // Act - Only sagas older than 1 hour
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        var stuckList = result.ToList();
        Assert.Single(stuckList);
        Assert.Equal(oldSaga.SagaId, stuckList[0].SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_Contract_RespectsBatchSize()
    {
        // Arrange - Create 5 stuck sagas
        for (int i = 0; i < 5; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"StuckSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow.AddHours(-5 - i),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5 - i),
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Request only 3
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 3);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public async Task GetStuckSagasAsync_Contract_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        var result = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10, cts.Token);
        Assert.NotNull(result);
    }

    #endregion

    #region Contract: SaveChangesAsync

    [Fact]
    public async Task SaveChangesAsync_Contract_CompletesSuccessfully()
    {
        // Act & Assert - Should not throw
        await _store.SaveChangesAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_Contract_SupportsCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw
        await _store.SaveChangesAsync(cts.Token);
    }

    #endregion

    #region Contract: ISagaState Properties

    [Fact]
    public async Task ISagaState_Contract_AllPropertiesAccessible()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-2);
        var updatedAt = DateTime.UtcNow.AddMinutes(-30);
        var completedAt = DateTime.UtcNow;

        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "PropertyTestSaga",
            Data = "{\"prop\":\"value\"}",
            Status = "Completed",
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = updatedAt,
            CompletedAtUtc = completedAt,
            ErrorMessage = "Test error",
            CurrentStep = 7
        };
        await _store.AddAsync(saga);

        // Act
        var retrieved = await _store.GetAsync(sagaId);

        // Assert - All properties accessible via interface
        Assert.NotNull(retrieved);
        Assert.Equal(sagaId, retrieved.SagaId);
        Assert.Equal("PropertyTestSaga", retrieved.SagaType);
        Assert.Equal("{\"prop\":\"value\"}", retrieved.Data);
        Assert.Equal("Completed", retrieved.Status);
        Assert.True(retrieved.StartedAtUtc != default);
        Assert.True(retrieved.LastUpdatedAtUtc != default);
        Assert.NotNull(retrieved.CompletedAtUtc);
        Assert.Equal("Test error", retrieved.ErrorMessage);
        Assert.Equal(7, retrieved.CurrentStep);
    }

    #endregion
}
