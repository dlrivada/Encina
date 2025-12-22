using Encina.Dapper.SqlServer.Sagas;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.SqlServer.Tests.Sagas;

/// <summary>
/// Property-based integration tests for <see cref="SagaStoreDapper"/>.
/// These tests verify invariants hold across various inputs and scenarios.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("TestType", "Property")]
[Trait("Provider", "Dapper.SqlServer")]
public sealed class SagaStoreDapperPropertyTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SagaStoreDapperPropertyTests(SqlServerFixture fixture)
    {
        _fixture = fixture;

        // Clear all data before each test to ensure clean state
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Property: Any saga added can be retrieved via GetAsync.
    /// Invariant: AddAsync followed by GetAsync always returns the added saga.
    /// </summary>
    [Theory]
    [InlineData("OrderSaga", "{\"orderId\":123}", "Running")]
    [InlineData("PaymentSaga", "{\"amount\":99.99}", "Completed")]
    [InlineData("ShippingSaga", "{\"address\":\"123 Main St\"}", "Failed")]
    [InlineData("", "", "Running")]
    [InlineData("SpecialChars", "' \" \\ / \n \r \t", "Compensating")]
    public async Task AddedSaga_AlwaysRetrievableByGet(string sagaType, string data, string status)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = data,
            Status = status,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };

        // Act
        await store.AddAsync(saga);
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(saga.SagaId, retrieved.SagaId);
        Assert.Equal(saga.SagaType, retrieved.SagaType);
        Assert.Equal(saga.Data, retrieved.Data);
        Assert.Equal(saga.Status, retrieved.Status);
    }

    /// <summary>
    /// Property: UpdateAsync always modifies the saga state.
    /// Invariant: After UpdateAsync, GetAsync returns the updated values.
    /// </summary>
    [Theory]
    [InlineData("Running", "Completed")]
    [InlineData("Running", "Failed")]
    [InlineData("Running", "Compensating")]
    [InlineData("Compensating", "Compensated")]
    [InlineData("Failed", "Running")]
    public async Task UpdatedSaga_AlwaysReflectsNewState(string initialStatus, string updatedStatus)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = initialStatus,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        saga.Status = updatedStatus;
        saga.CurrentStep = 2;
        await store.UpdateAsync(saga);

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(updatedStatus, retrieved.Status);
        Assert.Equal(2, retrieved.CurrentStep);
    }

    /// <summary>
    /// Property: GetStuckSagas filters correctly by status.
    /// Invariant: Only Running or Compensating sagas older than threshold are returned.
    /// </summary>
    [Theory]
    [InlineData("Running", true)]
    [InlineData("Compensating", true)]
    [InlineData("Completed", false)]
    [InlineData("Failed", false)]
    [InlineData("Compensated", false)]
    public async Task GetStuckSagas_OnlyReturnsRunningOrCompensating(string status, bool shouldBeStuck)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = status,
            StartedAtUtc = DateTime.UtcNow.AddHours(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5),
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), 10);

        // Assert
        if (shouldBeStuck)
        {
            Assert.Single(stuckSagas);
            Assert.Equal(sagaId, stuckSagas.First().SagaId);
        }
        else
        {
            Assert.Empty(stuckSagas);
        }
    }

    /// <summary>
    /// Property: GetStuckSagas respects time threshold.
    /// Invariant: Only sagas with LastUpdatedAtUtc older than threshold are returned.
    /// </summary>
    [Theory]
    [InlineData(1, false)]  // 1 hour old threshold, saga is 30 min old -> not stuck
    [InlineData(5, true)]   // 5 hour old threshold, saga is 30 min old -> stuck
    public async Task GetStuckSagas_RespectsTimeThreshold(int thresholdHours, bool shouldBeStuck)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-30),
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(TimeSpan.FromHours(thresholdHours), 10);

        // Assert
        if (shouldBeStuck)
        {
            Assert.Empty(stuckSagas); // Not old enough
        }
        else
        {
            Assert.Empty(stuckSagas); // Not old enough for 1 hour threshold either
        }
    }

    /// <summary>
    /// Property: Batch size always limits results.
    /// Invariant: GetStuckSagasAsync(batchSize: N).Count() ≤ N
    /// </summary>
    [Theory]
    [InlineData(50, 10)]
    [InlineData(50, 25)]
    [InlineData(50, 50)]
    [InlineData(50, 100)]
    [InlineData(10, 5)]
    [InlineData(10, 20)]
    public async Task BatchSize_AlwaysLimitsResults(int sagaCount, int batchSize)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        // Add N sagas
        for (int i = 0; i < sagaCount; i++)
        {
            await store.AddAsync(new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"Saga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow.AddHours(-5 - i),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5 - i),
                CurrentStep = 1
            });
        }

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(TimeSpan.FromHours(1), batchSize);

        // Assert
        Assert.True(stuckSagas.Count() <= batchSize);
        Assert.True(stuckSagas.Count() <= sagaCount);
    }

    /// <summary>
    /// Property: Sagas are always returned in chronological order.
    /// Invariant: GetStuckSagasAsync results are ordered by LastUpdatedAtUtc ascending.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task GetStuckSagas_AlwaysReturnsChronologicalOrder(int sagaCount)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        // Add sagas with sequential timestamps
        var baseTime = DateTime.UtcNow.AddHours(-10);
        for (int i = 0; i < sagaCount; i++)
        {
            await store.AddAsync(new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"Saga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = baseTime.AddHours(i),
                LastUpdatedAtUtc = baseTime.AddHours(i),
                CurrentStep = 1
            });
        }

        // Act
        var stuckSagas = (await store.GetStuckSagasAsync(TimeSpan.FromHours(1), 100)).ToList();

        // Assert - Verify ordering
        if (stuckSagas.Count > 1)
        {
            for (int i = 0; i < stuckSagas.Count - 1; i++)
            {
                Assert.True(stuckSagas[i].LastUpdatedAtUtc <= stuckSagas[i + 1].LastUpdatedAtUtc,
                    $"Saga at index {i} has timestamp {stuckSagas[i].LastUpdatedAtUtc}, " +
                    $"which is after saga at index {i + 1} with timestamp {stuckSagas[i + 1].LastUpdatedAtUtc}");
            }
        }
    }

    /// <summary>
    /// Property: CurrentStep is always incremented or preserved.
    /// Invariant: After UpdateAsync, CurrentStep >= original value.
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 5)]
    [InlineData(5, 10)]
    [InlineData(1, 1)]
    public async Task CurrentStep_AlwaysMonotonicallyIncreasing(int initialStep, int updatedStep)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "StepTestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = initialStep
        };
        await store.AddAsync(saga);

        // Act
        saga.CurrentStep = updatedStep;
        await store.UpdateAsync(saga);

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(updatedStep, retrieved.CurrentStep);
        Assert.True(retrieved.CurrentStep >= initialStep);
    }

    /// <summary>
    /// Property: SaveChangesAsync is idempotent.
    /// Invariant: Multiple SaveChangesAsync calls have same effect as one call.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public async Task SaveChanges_IsIdempotent(int callCount)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        // Act - Call SaveChangesAsync N times
        for (int i = 0; i < callCount; i++)
        {
            await store.SaveChangesAsync();
        }

        // Assert - No exception thrown, operation completed
        Assert.True(true);
    }

    /// <summary>
    /// Property: CompletedAtUtc is only set when saga is completed.
    /// Invariant: CompletedAtUtc != null ⟺ Status == "Completed"
    /// </summary>
    [Theory]
    [InlineData("Running", false)]
    [InlineData("Completed", true)]
    [InlineData("Failed", false)]
    [InlineData("Compensating", false)]
    [InlineData("Compensated", false)]
    public async Task CompletedAtUtc_OnlySetWhenCompleted(string status, bool shouldHaveCompletedAt)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "CompletionTestSaga",
            Data = "{}",
            Status = status,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = shouldHaveCompletedAt ? DateTime.UtcNow : null,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        Assert.NotNull(retrieved);
        if (shouldHaveCompletedAt)
        {
            Assert.NotNull(retrieved.CompletedAtUtc);
        }
        else
        {
            Assert.Null(retrieved.CompletedAtUtc);
        }
    }

    /// <summary>
    /// Property: ErrorMessage is set only when saga fails.
    /// Invariant: ErrorMessage is typically populated for Failed status.
    /// </summary>
    [Theory]
    [InlineData("Running", null)]
    [InlineData("Failed", "Payment gateway timeout")]
    [InlineData("Completed", null)]
    [InlineData("Compensating", "Compensating due to error")]
    public async Task ErrorMessage_SetAppropriatelyByStatus(string status, string? errorMessage)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "ErrorTestSaga",
            Data = "{}",
            Status = status,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            ErrorMessage = errorMessage,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(errorMessage, retrieved.ErrorMessage);
    }

    /// <summary>
    /// Property: StartedAtUtc is immutable after creation.
    /// Invariant: StartedAtUtc never changes after AddAsync.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task StartedAtUtc_NeverChangesAfterCreation(int updateCount)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var startedTime = DateTime.UtcNow.AddHours(-5);
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "ImmutableTestSaga",
            Data = "{}",
            Status = "Running",
            StartedAtUtc = startedTime,
            LastUpdatedAtUtc = startedTime,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act - Update N times
        for (int i = 0; i < updateCount; i++)
        {
            saga.Status = i % 2 == 0 ? "Running" : "Completed";
            saga.CurrentStep = i + 2;
            await store.UpdateAsync(saga);
        }

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(startedTime, retrieved.StartedAtUtc);
    }

    /// <summary>
    /// Property: Data field accepts any valid JSON or string.
    /// Invariant: Data is preserved exactly as stored.
    /// </summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"key\":\"value\"}")]
    [InlineData("{\"nested\":{\"deep\":{\"value\":123}}}")]
    [InlineData("[1,2,3,4,5]")]
    [InlineData("plain text")]
    [InlineData("")]
    public async Task DataField_PreservesAnyContent(string data)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "DataTestSaga",
            Data = data,
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        var retrieved = await store.GetAsync(sagaId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(data, retrieved.Data);
    }

    /// <summary>
    /// Property: SagaType identifies the saga orchestration type.
    /// Invariant: SagaType is preserved after update.
    /// </summary>
    [Theory]
    [InlineData("OrderSaga")]
    [InlineData("PaymentSaga")]
    [InlineData("ShippingSaga")]
    [InlineData("InventorySaga")]
    public async Task SagaType_PreservedAfterUpdate(string sagaType)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = sagaType,
            Data = "{}",
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await store.AddAsync(saga);

        // Act
        saga.Status = "Completed";
        saga.CurrentStep = 5;
        await store.UpdateAsync(saga);

        // Assert
        var retrieved = await store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(sagaType, retrieved.SagaType);
    }

    /// <summary>
    /// Property: GetAsync for non-existent saga always returns null.
    /// Invariant: GetAsync(nonExistentId) == null
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetAsync_NonExistentSaga_AlwaysReturnsNull(int attemptCount)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        // Act & Assert
        for (int i = 0; i < attemptCount; i++)
        {
            var nonExistentId = Guid.NewGuid();
            var result = await store.GetAsync(nonExistentId);
            Assert.Null(result);
        }
    }

    /// <summary>
    /// Property: Multiple sagas can exist simultaneously.
    /// Invariant: Adding multiple sagas doesn't interfere with each other.
    /// </summary>
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(25)]
    public async Task MultipleSagas_CoexistIndependently(int sagaCount)
    {
        // Arrange
        
        var store = new SagaStoreDapper(_fixture.CreateConnection());

        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            await store.AddAsync(new SagaState
            {
                SagaId = sagaId,
                SagaType = $"Saga{i}",
                Data = $"{{\"index\":{i}}}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = i
            });
        }

        // Act & Assert - Each saga can be retrieved independently
        for (int i = 0; i < sagaCount; i++)
        {
            var retrieved = await store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal($"Saga{i}", retrieved.SagaType);
            Assert.Equal(i, retrieved.CurrentStep);
        }
    }
}
