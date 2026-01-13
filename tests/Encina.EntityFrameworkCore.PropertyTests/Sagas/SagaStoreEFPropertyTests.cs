using System.Diagnostics.CodeAnalysis;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.PropertyTests.Sagas;

/// <summary>
/// Property-based tests for <see cref="SagaStoreEF"/>.
/// Verifies invariants that MUST hold for ALL possible inputs.
/// </summary>
[Trait("Category", "Property")]
[SuppressMessage("Usage", "CA1001:Types that own disposable fields should be disposable", Justification = "IAsyncLifetime handles disposal via DisposeAsync")]
public sealed class SagaStoreEFPropertyTests : IAsyncLifetime
{
    private TestDbContext? _dbContext;
    private SagaStoreEF? _store;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"SagaPropertyTests_{Guid.NewGuid()}")
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new SagaStoreEF(_dbContext);

        await _dbContext.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Property: A saga that is added can ALWAYS be retrieved by its SagaId.
    /// </summary>
    [Fact]
    public async Task Property_AddThenGet_AlwaysRetrievableById()
    {
        // Generate random test cases
        var testCases = Enumerable.Range(0, 20).Select(_ => new
        {
            SagaId = Guid.NewGuid(),
            SagaType = $"TestSaga_{Guid.NewGuid()}",
            Data = $"{{\"step\":{Random.Shared.Next(1, 10)}}}"
        }).ToList();

        foreach (var testCase in testCases)
        {
            // Arrange
            var saga = new SagaState
            {
                SagaId = testCase.SagaId,
                SagaType = testCase.SagaType,
                Data = testCase.Data,
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            // Act
            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Assert
            var retrieved = await _store.GetAsync(testCase.SagaId);
            retrieved.ShouldNotBeNull("added saga must ALWAYS be retrievable");
            retrieved!.SagaId.ShouldBe(testCase.SagaId);
            retrieved.SagaType.ShouldBe(testCase.SagaType);
            retrieved.Data.ShouldBe(testCase.Data);
        }
    }

    /// <summary>
    /// Property: UpdateAsync ALWAYS updates LastUpdatedAtUtc.
    /// </summary>
    [Fact]
    public async Task Property_Update_AlwaysUpdatesTimestamp()
    {
        var testCases = Enumerable.Range(0, 15).Select(i => new
        {
            NewData = $"{{\"step\":{i},\"value\":\"{Guid.NewGuid()}\"}}",
            NewStep = i
        }).ToList();

        foreach (var testCase in testCases)
        {
            await ClearDatabase();

            // Arrange - set original time in the past to ensure clear difference
            var originalTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "TestSaga",
                Data = "{\"initial\":true}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = originalTime,
                LastUpdatedAtUtc = originalTime
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act - no delay needed, just update
            saga.Data = testCase.NewData;
            saga.CurrentStep = testCase.NewStep;
            await _store.UpdateAsync(saga);
            await _store.SaveChangesAsync();

            // Assert
            var updated = await _store.GetAsync(saga.SagaId);
            updated.ShouldNotBeNull();
            updated!.LastUpdatedAtUtc.ShouldBeGreaterThan(originalTime,
                "UpdateAsync must update LastUpdatedAtUtc to a time after originalTime");
            updated.Data.ShouldBe(testCase.NewData);
            updated.CurrentStep.ShouldBe(testCase.NewStep);
        }
    }

    /// <summary>
    /// Property: Stuck sagas ALWAYS returned when older than threshold.
    /// </summary>
    [Fact]
    public async Task Property_StuckDetection_OlderThanThresholdAlwaysReturned()
    {
        var now = DateTime.UtcNow;
        var threshold = TimeSpan.FromMinutes(30);

        var testCases = new[]
        {
            // (LastUpdated, Status, ShouldAppear)
            (LastUpdated: now.AddMinutes(-60), Status: SagaStatus.Running, ShouldAppear: true),
            (LastUpdated: now.AddMinutes(-31), Status: SagaStatus.Running, ShouldAppear: true),
            (LastUpdated: now.AddMinutes(-30), Status: SagaStatus.Running, ShouldAppear: true),
            (LastUpdated: now.AddMinutes(-29), Status: SagaStatus.Running, ShouldAppear: false),
            (LastUpdated: now.AddMinutes(-5), Status: SagaStatus.Running, ShouldAppear: false),
            (LastUpdated: now.AddMinutes(-60), Status: SagaStatus.Compensating, ShouldAppear: true),
            (LastUpdated: now.AddMinutes(-60), Status: SagaStatus.Completed, ShouldAppear: false),
            (LastUpdated: now.AddMinutes(-60), Status: SagaStatus.Compensated, ShouldAppear: false),
            (LastUpdated: now.AddMinutes(-60), Status: SagaStatus.Failed, ShouldAppear: false)
        };

        foreach (var (lastUpdated, status, shouldAppear) in testCases)
        {
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "StuckTest",
                Data = "{}",
                CurrentStep = 0,
                Status = status,
                StartedAtUtc = lastUpdated,
                LastUpdatedAtUtc = lastUpdated
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act
            var stuck = await _store.GetStuckSagasAsync(threshold, 100);

            // Assert
            if (shouldAppear)
            {
                stuck.ShouldContain(s => s.SagaId == saga.SagaId,
                    $"saga with status={status} and LastUpdated={lastUpdated} should be stuck");
            }
            else
            {
                stuck.ShouldNotContain(s => s.SagaId == saga.SagaId,
                    $"saga with status={status} and LastUpdated={lastUpdated} should NOT be stuck");
            }

            await ClearDatabase();
        }
    }

    /// <summary>
    /// Property: Status transitions ALWAYS preserved.
    /// </summary>
    [Fact]
    public async Task Property_StatusTransitions_AlwaysPreserved()
    {
        var statusTransitions = new[]
        {
            SagaStatus.Running,
            SagaStatus.Compensating,
            SagaStatus.Compensated,
            SagaStatus.Completed,
            SagaStatus.Failed,
            SagaStatus.TimedOut
        };

        foreach (var targetStatus in statusTransitions)
        {
            await ClearDatabase();

            // Arrange
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "StatusTest",
                Data = "{}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act - transition to target status
            saga.Status = targetStatus;
            if (targetStatus == SagaStatus.Completed || targetStatus == SagaStatus.Compensated)
            {
                saga.CompletedAtUtc = DateTime.UtcNow;
            }
            await _store.UpdateAsync(saga);
            await _store.SaveChangesAsync();

            // Assert
            var updated = await _store.GetAsync(saga.SagaId);
            updated.ShouldNotBeNull();
            updated!.Status.ToString().ShouldBe(targetStatus.ToString(),
                $"status transition to {targetStatus} must ALWAYS be preserved");
        }
    }

    /// <summary>
    /// Property: GetStuckSagas ALWAYS respects batch size.
    /// </summary>
    [Fact]
    public async Task Property_BatchSize_AlwaysRespectsLimit()
    {
        var batchSizes = new[] { 1, 5, 10, 25 };

        foreach (var batchSize in batchSizes)
        {
            await ClearDatabase();

            // Create more stuck sagas than batch size
            var sagaCount = batchSize + Random.Shared.Next(5, 15);
            var oldTime = DateTime.UtcNow.AddHours(-2);

            for (int i = 0; i < sagaCount; i++)
            {
                await _store!.AddAsync(new SagaState
                {
                    SagaId = Guid.NewGuid(),
                    SagaType = "BatchTest",
                    Data = $"{{\"index\":{i}}}",
                    CurrentStep = i,
                    Status = SagaStatus.Running,
                    StartedAtUtc = oldTime,
                    LastUpdatedAtUtc = oldTime
                });
            }
            await _store!.SaveChangesAsync();

            // Act
            var stuck = await _store!.GetStuckSagasAsync(TimeSpan.FromMinutes(30), batchSize);

            // Assert
            stuck.Count().ShouldBeLessThanOrEqualTo(batchSize,
                $"batch size {batchSize} must ALWAYS be respected");
        }
    }

    /// <summary>
    /// Property: Stuck sagas ALWAYS ordered by LastUpdatedAtUtc ascending.
    /// </summary>
    [Fact]
    public async Task Property_Ordering_StuckOrderedByLastUpdatedUtc()
    {
        var baseTime = DateTime.UtcNow.AddHours(-10);

        // Create stuck sagas with random last updated times
        var sagas = Enumerable.Range(0, 20)
            .Select(i =>
            {
                var lastUpdated = baseTime.AddMinutes(Random.Shared.Next(-100, -30));
                return new SagaState
                {
                    SagaId = Guid.NewGuid(),
                    SagaType = "OrderTest",
                    Data = $"{{\"index\":{i}}}",
                    CurrentStep = i,
                    Status = SagaStatus.Running,
                    StartedAtUtc = lastUpdated,
                    LastUpdatedAtUtc = lastUpdated
                };
            })
            .OrderBy(_ => Random.Shared.Next()) // Randomize insertion order
            .ToList();

        foreach (var saga in sagas)
        {
            await _store!.AddAsync(saga);
        }
        await _store!.SaveChangesAsync();

        // Act
        var stuck = (await _store!.GetStuckSagasAsync(
            TimeSpan.FromMinutes(20), 100)).ToList();

        // Assert - must be ordered by LastUpdatedAtUtc ascending
        for (int i = 1; i < stuck.Count; i++)
        {
            stuck[i].LastUpdatedAtUtc.ShouldBeGreaterThanOrEqualTo(stuck[i - 1].LastUpdatedAtUtc,
                "stuck sagas must ALWAYS be ordered by LastUpdatedAtUtc ascending");
        }
    }

    /// <summary>
    /// Property: CorrelationId ALWAYS preserved across updates.
    /// </summary>
    [Fact]
    public async Task Property_CorrelationId_AlwaysPreserved()
    {
        var correlationIds = Enumerable.Range(0, 10)
            .Select(_ => Guid.NewGuid().ToString())
            .ToList();

        foreach (var correlationId in correlationIds)
        {
            await ClearDatabase();

            // Arrange
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "CorrelationTest",
                Data = "{}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                CorrelationId = correlationId
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act - multiple updates
            for (int step = 1; step <= 5; step++)
            {
                saga.CurrentStep = step;
                saga.Data = $"{{\"step\":{step}}}";
                await _store.UpdateAsync(saga);
                await _store.SaveChangesAsync();

                // Assert
                var updated = await _store.GetAsync(saga.SagaId);
                updated.ShouldNotBeNull();
                ((SagaState)updated!).CorrelationId.ShouldBe(correlationId,
                    "CorrelationId must ALWAYS be preserved across updates");
            }
        }
    }

    /// <summary>
    /// Property: TimeoutAtUtc ALWAYS preserved if set.
    /// </summary>
    [Fact]
    public async Task Property_TimeoutAtUtc_AlwaysPreserved()
    {
        var timeouts = new[]
        {
            DateTime.UtcNow.AddMinutes(5),
            DateTime.UtcNow.AddHours(1),
            DateTime.UtcNow.AddDays(1)
        };

        foreach (var timeout in timeouts)
        {
            await ClearDatabase();

            // Arrange
            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "TimeoutTest",
                Data = "{}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                TimeoutAtUtc = timeout
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act - update
            saga.CurrentStep = 1;
            await _store.UpdateAsync(saga);
            await _store.SaveChangesAsync();

            // Assert
            var updated = await _store.GetAsync(saga.SagaId);
            updated.ShouldNotBeNull();
            ((SagaState)updated!).TimeoutAtUtc.ShouldBe(timeout,
                "TimeoutAtUtc must ALWAYS be preserved across updates");
        }
    }

    /// <summary>
    /// Property: CurrentStep ALWAYS reflects latest update.
    /// </summary>
    [Fact]
    public async Task Property_CurrentStep_AlwaysReflectsUpdate()
    {
        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StepTest",
            Data = "{}",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _store!.AddAsync(saga);
        await _store.SaveChangesAsync();

        // Test step progression
        for (int step = 1; step <= 20; step++)
        {
            saga.CurrentStep = step;
            await _store.UpdateAsync(saga);
            await _store.SaveChangesAsync();

            var retrieved = await _store.GetAsync(saga.SagaId);
            retrieved.ShouldNotBeNull();
            retrieved!.CurrentStep.ShouldBe(step,
                $"CurrentStep must ALWAYS reflect latest update (step {step})");
        }
    }

    /// <summary>
    /// Property: CompletedAtUtc ALWAYS set when status is terminal.
    /// </summary>
    [Fact]
    public async Task Property_CompletedAt_SetOnTerminalStatus()
    {
        var terminalStatuses = new[]
        {
            SagaStatus.Completed,
            SagaStatus.Compensated,
            SagaStatus.Failed
        };

        foreach (var terminalStatus in terminalStatuses)
        {
            await ClearDatabase();

            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "CompletionTest",
                Data = "{}",
                CurrentStep = 5,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Act - transition to terminal status
            saga.Status = terminalStatus;
            saga.CompletedAtUtc = DateTime.UtcNow;
            await _store.UpdateAsync(saga);
            await _store.SaveChangesAsync();

            // Assert
            var completed = await _store.GetAsync(saga.SagaId);
            completed.ShouldNotBeNull();
            completed!.CompletedAtUtc.ShouldNotBeNull(
                $"CompletedAtUtc should be set when status is {terminalStatus}");
            completed.Status.ToString().ShouldBe(terminalStatus.ToString());
        }
    }

    /// <summary>
    /// Property: Concurrent updates to different sagas ALWAYS succeed.
    /// </summary>
    [Fact]
    public async Task Property_ConcurrentUpdates_AlwaysSucceed()
    {
        const int concurrentSagas = 30;

        // Create sagas
        var sagas = Enumerable.Range(0, concurrentSagas)
            .Select(i => new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"ConcurrentTest_{i}",
                Data = $"{{\"index\":{i}}}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        foreach (var saga in sagas)
        {
            await _store!.AddAsync(saga);
        }
        await _store!.SaveChangesAsync();

        // Act - concurrent updates
        var tasks = sagas.Select(async saga =>
        {
            saga.CurrentStep = 1;
            saga.Data = $"{{\"updated\":true}}";
            await _store!.UpdateAsync(saga);
            await _store.SaveChangesAsync();
        });

        await Task.WhenAll(tasks);

        // Assert - all sagas must be updated
        foreach (var saga in sagas)
        {
            var retrieved = await _store!.GetAsync(saga.SagaId);
            retrieved.ShouldNotBeNull();
            retrieved!.CurrentStep.ShouldBe(1,
                "concurrent updates to different sagas must ALL succeed");
            retrieved.Data.ShouldBe("{\"updated\":true}");
        }
    }

    /// <summary>
    /// Property: Metadata ALWAYS preserved across updates.
    /// </summary>
    [Fact]
    public async Task Property_Metadata_AlwaysPreserved()
    {
        var metadataValues = new[]
        {
            "{\"userId\":\"123\",\"tenantId\":\"abc\"}",
            "{\"source\":\"api\",\"version\":\"1.0\"}",
            null  // No metadata
        };

        foreach (var metadata in metadataValues)
        {
            await ClearDatabase();

            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = "MetadataTest",
                Data = "{}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow,
                Metadata = metadata
            };

            await _store!.AddAsync(saga);
            await _store.SaveChangesAsync();

            // Update multiple times
            for (int i = 1; i <= 3; i++)
            {
                saga.CurrentStep = i;
                await _store.UpdateAsync(saga);
                await _store.SaveChangesAsync();

                var retrieved = await _store.GetAsync(saga.SagaId);
                retrieved.ShouldNotBeNull();
                ((SagaState)retrieved!).Metadata.ShouldBe(metadata,
                    "Metadata must ALWAYS be preserved across updates");
            }
        }
    }

    private async Task ClearDatabase()
    {
        var allSagas = await _dbContext!.Set<SagaState>().ToListAsync();
        _dbContext.Set<SagaState>().RemoveRange(allSagas);
        await _dbContext.SaveChangesAsync();
    }

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<SagaState> SagaStates => Set<SagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SagaState>(entity =>
            {
                entity.HasKey(e => e.SagaId);
                entity.Property(e => e.SagaType).IsRequired();
                entity.Property(e => e.Data).IsRequired();
            });
        }
    }
}
