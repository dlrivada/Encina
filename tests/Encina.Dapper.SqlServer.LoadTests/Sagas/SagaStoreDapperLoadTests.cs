using Encina.Dapper.SqlServer.Sagas;
using Encina.Messaging.Sagas;
using Encina.TestInfrastructure.Extensions;
using Encina.TestInfrastructure.Fixtures;
using Xunit;

namespace Encina.Dapper.SqlServer.Tests.Sagas;

/// <summary>
/// Load tests for <see cref="SagaStoreDapper"/>.
/// Verifies behavior under concurrency, volume, and stress conditions.
/// </summary>
[Trait("Category", "Load")]
public sealed class SagaStoreDapperLoadTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _database;
    private readonly SagaStoreDapper _store;

    public SagaStoreDapperLoadTests(SqlServerFixture database)
    {
        _database = database;
        

        // Clear all data before each test to ensure clean state
        _database.ClearAllDataAsync().GetAwaiter().GetResult();

        _store = new SagaStoreDapper(_database.CreateConnection());
    }

    #region Concurrency Tests

    [Fact]
    public async Task AddAsync_ConcurrentWrites_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task<Guid>>();
        const int concurrentWrites = 50;

        // Act - Write 50 sagas concurrently
        for (int i = 0; i < concurrentWrites; i++)
        {
            var index = i; // Capture for closure
            tasks.Add(Task.Run(async () =>
            {
                var sagaId = Guid.NewGuid();
                var saga = new SagaState
                {
                    SagaId = sagaId,
                    SagaType = $"ConcurrentSaga{index}",
                    Data = "{\"test\":true}",
                    Status = "Running",
                    StartedAtUtc = DateTime.UtcNow,
                    LastUpdatedAtUtc = DateTime.UtcNow,
                    CurrentStep = 1
                };
                await _store.AddAsync(saga);
                return sagaId;
            }));
        }

        var sagaIds = await Task.WhenAll(tasks);

        // Assert - All sagas persisted (verify by retrieving each)
        foreach (var sagaId in sagaIds)
        {
            var retrieved = await _store.GetAsync(sagaId);
            Assert.NotNull(retrieved);
        }
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentUpdates_AllSucceed()
    {
        // Arrange - Create 30 sagas
        const int sagaCount = 30;
        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"UpdateSaga{i}",
                Data = "{\"step\":1}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Update all concurrently
        var tasks = new List<Task>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = sagaIds[i];
            tasks.Add(Task.Run(async () =>
            {
                var saga = await _store.GetAsync(sagaId);
                if (saga != null)
                {
                    saga.Status = "Completed";
                    saga.CurrentStep = 5;
                    saga.CompletedAtUtc = DateTime.UtcNow;
                    await _store.UpdateAsync(saga);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - All marked as completed
        for (int i = 0; i < sagaCount; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal("Completed", retrieved.Status);
        }
    }

    [Fact]
    public async Task UpdateAsync_ConcurrentStatusChanges_AllIncrement()
    {
        // Arrange - Create 20 sagas
        const int sagaCount = 20;
        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"StatusSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 0
            };
            await _store.AddAsync(saga);
        }

        // Act - Increment CurrentStep 5 times concurrently for each saga
        const int incrementsPerSaga = 5;
        for (int step = 0; step < incrementsPerSaga; step++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < sagaCount; i++)
            {
                var sagaId = sagaIds[i];
                tasks.Add(Task.Run(async () =>
                {
                    var saga = await _store.GetAsync(sagaId);
                    if (saga != null)
                    {
                        saga.CurrentStep++;
                        saga.LastUpdatedAtUtc = DateTime.UtcNow;
                        await _store.UpdateAsync(saga);
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        // Assert - All have correct step count
        for (int i = 0; i < sagaCount; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal(incrementsPerSaga, retrieved.CurrentStep);
        }
    }

    #endregion

    #region Volume Tests

    [Fact]
    public async Task AddAsync_LargeVolume_AllPersist()
    {
        // Arrange & Act - Add 500 sagas
        const int sagaCount = 500;
        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"VolumeSaga{i}",
                Data = "{\"volume\":true}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Assert - Verify random samples
        var sampleIndices = new[] { 0, 100, 250, 400, 499 };
        foreach (var index in sampleIndices)
        {
            var retrieved = await _store.GetAsync(sagaIds[index]);
            Assert.NotNull(retrieved);
        }
    }

    [Fact]
    public async Task GetStuckSagasAsync_LargeBatch_ReturnsCorrectly()
    {
        // Arrange - Create 200 stuck sagas
        const int stuckCount = 200;
        for (int i = 0; i < stuckCount; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"StuckSaga{i}",
                Data = "{}",
                Status = i % 2 == 0 ? "Running" : "Compensating",
                StartedAtUtc = DateTime.UtcNow.AddHours(-5),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-5 - i),
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Get all stuck (batch size = 500)
        var stuck = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 500);

        // Assert
        Assert.Equal(stuckCount, stuck.Count());
    }

    [Fact]
    public async Task GetStuckSagasAsync_BatchSizeFiltering_ReturnsCorrectCount()
    {
        // Arrange - Create 150 stuck sagas
        const int stuckCount = 150;
        for (int i = 0; i < stuckCount; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"BatchSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow.AddHours(-10),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-10 - i),
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Request only 50
        var stuck = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 50);

        // Assert
        Assert.Equal(50, stuck.Count());
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task MixedOperations_HighConcurrency_NoDataCorruption()
    {
        // Arrange - Create 30 sagas
        const int sagaCount = 30;
        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"StressSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Mix of concurrent operations
        var tasks = new List<Task>();

        // 10 reads
        for (int i = 0; i < 10; i++)
        {
            var sagaId = sagaIds[i];
            tasks.Add(Task.Run(async () => await _store.GetAsync(sagaId)));
        }

        // 10 updates (mark as completed)
        for (int i = 10; i < 20; i++)
        {
            var sagaId = sagaIds[i];
            tasks.Add(Task.Run(async () =>
            {
                var saga = await _store.GetAsync(sagaId);
                if (saga != null)
                {
                    saga.Status = "Completed";
                    saga.CompletedAtUtc = DateTime.UtcNow;
                    await _store.UpdateAsync(saga);
                }
            }));
        }

        // 10 failures
        for (int i = 20; i < 30; i++)
        {
            var sagaId = sagaIds[i];
            tasks.Add(Task.Run(async () =>
            {
                var saga = await _store.GetAsync(sagaId);
                if (saga != null)
                {
                    saga.Status = "Failed";
                    saga.ErrorMessage = "Stress test error";
                    await _store.UpdateAsync(saga);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Data integrity maintained
        for (int i = 0; i < 10; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
        }

        for (int i = 10; i < 20; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal("Completed", retrieved.Status);
        }

        for (int i = 20; i < 30; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal("Failed", retrieved.Status);
            Assert.Equal("Stress test error", retrieved.ErrorMessage);
        }
    }

    [Fact]
    public async Task LargePayload_Processing_HandlesCorrectly()
    {
        // Arrange - Create saga with 50KB data payload
        var sagaId = Guid.NewGuid();
        var largeData = new string('X', 50_000);
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "LargePayloadSaga",
            Data = largeData,
            Status = "Running",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CurrentStep = 1
        };
        await _store.AddAsync(saga);

        // Act - Update with another large payload
        var updatedData = new string('Y', 50_000);
        saga.Data = updatedData;
        saga.CurrentStep = 2;
        await _store.UpdateAsync(saga);

        // Assert - Large payload persisted correctly
        var retrieved = await _store.GetAsync(sagaId);
        Assert.NotNull(retrieved);
        Assert.Equal(50_000, retrieved.Data.Length);
        Assert.Equal(updatedData, retrieved.Data);
    }

    [Fact]
    public async Task GetStuckSagas_HighVolume_PerformanceRemainsSteady()
    {
        // Arrange - Create 1000 sagas (500 stuck, 500 recent)
        const int totalSagas = 1000;
        for (int i = 0; i < totalSagas; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"PerfSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow.AddHours(i < 500 ? -5 : 0),
                LastUpdatedAtUtc = DateTime.UtcNow.AddHours(i < 500 ? -5 : 0),
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        // Act - Query stuck sagas
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var stuck = await _store.GetStuckSagasAsync(TimeSpan.FromHours(1), 100);
        stopwatch.Stop();

        // Assert - Performance acceptable (< 500ms for 1000 sagas)
        Assert.True(stopwatch.ElapsedMilliseconds < 500, $"Query took {stopwatch.ElapsedMilliseconds}ms");
        Assert.Equal(100, stuck.Count()); // Limited by batch size
    }

    #endregion

    #region Saga Lifecycle Under Load

    [Fact]
    public async Task SagaLifecycle_ConcurrentProgressions_MaintainConsistency()
    {
        // Arrange - Create 20 sagas
        const int sagaCount = 20;
        var sagaIds = new List<Guid>();
        for (int i = 0; i < sagaCount; i++)
        {
            var sagaId = Guid.NewGuid();
            sagaIds.Add(sagaId);
            var saga = new SagaState
            {
                SagaId = sagaId,
                SagaType = $"LifecycleSaga{i}",
                Data = "{\"step\":0}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 0
            };
            await _store.AddAsync(saga);
        }

        // Act - Simulate full lifecycle: Running → Step 1 → Step 2 → Step 3 → Completed
        const int totalSteps = 3;
        for (int step = 1; step <= totalSteps; step++)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < sagaCount; i++)
            {
                var sagaId = sagaIds[i];
                var currentStep = step;
                tasks.Add(Task.Run(async () =>
                {
                    var saga = await _store.GetAsync(sagaId);
                    if (saga != null)
                    {
                        saga.CurrentStep = currentStep;
                        saga.Data = $"{{\"step\":{currentStep}}}";
                        saga.LastUpdatedAtUtc = DateTime.UtcNow;
                        if (currentStep == totalSteps)
                        {
                            saga.Status = "Completed";
                            saga.CompletedAtUtc = DateTime.UtcNow;
                        }
                        await _store.UpdateAsync(saga);
                    }
                }));
            }
            await Task.WhenAll(tasks);
        }

        // Assert - All sagas completed successfully
        for (int i = 0; i < sagaCount; i++)
        {
            var retrieved = await _store.GetAsync(sagaIds[i]);
            Assert.NotNull(retrieved);
            Assert.Equal("Completed", retrieved.Status);
            Assert.Equal(totalSteps, retrieved.CurrentStep);
            Assert.NotNull(retrieved.CompletedAtUtc);
        }
    }

    #endregion

    #region Performance Degradation

    [Fact]
    public async Task SequentialWrites_Performance_LinearDegradation()
    {
        // Arrange & Act - Measure time for batches of 50
        var batch1Time = await MeasureWriteTime(0, 50);
        var batch2Time = await MeasureWriteTime(50, 100);

        // Assert - Performance should be relatively consistent
        // (Batch 2 should not be significantly slower than Batch 1)
        var degradationRatio = (double)batch2Time.TotalMilliseconds / batch1Time.TotalMilliseconds;
        Assert.True(degradationRatio < 2.0, $"Performance degraded by {degradationRatio:F2}x");
    }

    private async Task<TimeSpan> MeasureWriteTime(int startIndex, int endIndex)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = startIndex; i < endIndex; i++)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"PerfSaga{i}",
                Data = "{}",
                Status = "Running",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CurrentStep = 1
            };
            await _store.AddAsync(saga);
        }

        stopwatch.Stop();
        return stopwatch.Elapsed;
    }

    #endregion
}
