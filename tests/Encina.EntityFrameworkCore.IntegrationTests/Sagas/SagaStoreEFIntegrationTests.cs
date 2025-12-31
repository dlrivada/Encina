using Shouldly;
using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Xunit;

using SagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.IntegrationTests.Sagas;

/// <summary>
/// Integration tests for SagaStoreEF using real SQL Server via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class SagaStoreEFIntegrationTests : IClassFixture<EFCoreFixture>
{
    private readonly EFCoreFixture _fixture;

    public SagaStoreEFIntegrationTests(EFCoreFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearAllDataAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistSaga()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            Data = "{\"test\":\"data\"}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var stored = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        stored.ShouldNotBeNull();
        stored!.SagaType.ShouldBe("TestSaga");
        stored.CurrentStep.ShouldBe(0);
    }

    [Fact]
    public async Task GetAsync_WithExistingSaga_ShouldReturnSaga()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        var retrieved = await store.GetAsync(saga.SagaId);

        // Assert
        retrieved.ShouldNotBeNull();
        retrieved!.SagaId.ShouldBe(saga.SagaId);
    }

    [Fact]
    public async Task GetAsync_WithNonExistentSaga_ShouldReturnNull()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        // Act
        var retrieved = await store.GetAsync(Guid.NewGuid());

        // Assert
        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySagaState()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            Data = "{\"counter\":1}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        saga.CurrentStep = 1;
        saga.Data = "{\"counter\":2}";
        saga.LastUpdatedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.CurrentStep.ShouldBe(1);
        updated.Data.ShouldBe("{\"counter\":2}");
    }

    [Fact]
    public async Task UpdateAsync_ToCompleted_ShouldSetCompletionTimestamp()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 2,
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        saga.Status = SagaStatus.Completed;
        saga.CompletedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.Status.ShouldBe(SagaStatus.Completed);
        updated.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ToFailed_ShouldSetErrorInfo()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        saga.Status = SagaStatus.Failed;
        saga.ErrorMessage = "Test error occurred";
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.Status.ShouldBe(SagaStatus.Failed);
        updated.ErrorMessage.ShouldBe("Test error occurred");
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldReturnOldRunningSagas()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var stuck1 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 0,
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var stuck2 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3)
        };

        var completed = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = 2,
            Status = SagaStatus.Completed,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.AddRange(stuck1, stuck2, completed);
        await context.SaveChangesAsync();

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(olderThan: TimeSpan.FromHours(1), batchSize: 10);

        // Assert
        var sagaList = stuckSagas.ToList();
        sagaList.Count.ShouldBe(2);
        sagaList.ShouldContain(s => s.SagaId == stuck1.SagaId);
        sagaList.ShouldContain(s => s.SagaId == stuck2.SagaId);
    }

    [Fact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            using var context = _fixture.CreateDbContext();
            var store = new SagaStoreEF(context);

            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"ConcurrentSaga{i}",
                CurrentStep = 0,
                Status = SagaStatus.Running,
                Data = $"{{\"index\":{i}}}",
                StartedAtUtc = DateTime.UtcNow,
                LastUpdatedAtUtc = DateTime.UtcNow
            };

            await store.AddAsync(saga);
            await store.SaveChangesAsync();
            return saga.SagaId;
        });

        // Act
        var sagaIds = await Task.WhenAll(tasks);

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        foreach (var id in sagaIds)
        {
            var stored = await verifyContext.SagaStates.FindAsync(id);
            stored.ShouldNotBeNull();
        }
    }
}
