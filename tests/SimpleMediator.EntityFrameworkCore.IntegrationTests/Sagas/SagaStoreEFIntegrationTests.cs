using FluentAssertions;
using SimpleMediator.EntityFrameworkCore.Sagas;
using SimpleMediator.Messaging.Sagas;
using Xunit;

namespace SimpleMediator.EntityFrameworkCore.IntegrationTests.Sagas;

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
            CurrentStep = "Step1",
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
        stored.Should().NotBeNull();
        stored!.SagaType.Should().Be("TestSaga");
        stored.CurrentStep.Should().Be("Step1");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingSaga_ShouldReturnSaga()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "Step1",
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        var retrieved = await store.GetByIdAsync(saga.SagaId);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.SagaId.Should().Be(saga.SagaId);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentSaga_ShouldReturnNull()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        // Act
        var retrieved = await store.GetByIdAsync(Guid.NewGuid());

        // Assert
        retrieved.Should().BeNull();
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
            CurrentStep = "Step1",
            Status = SagaStatus.Running,
            Data = "{\"counter\":1}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        saga.CurrentStep = "Step2";
        saga.Data = "{\"counter\":2}";
        saga.LastUpdatedAtUtc = DateTime.UtcNow;
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.CurrentStep.Should().Be("Step2");
        updated.Data.Should().Be("{\"counter\":2}");
    }

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldSetCompletionTimestamp()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "FinalStep",
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsCompletedAsync(saga.SagaId);
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.Status.Should().Be(SagaStatus.Completed);
        updated.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldSetErrorInfo()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "FailedStep",
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.Add(saga);
        await context.SaveChangesAsync();

        // Act
        await store.MarkAsFailedAsync(saga.SagaId, "Test error occurred");
        await store.SaveChangesAsync();

        // Assert
        using var verifyContext = _fixture.CreateDbContext();
        var updated = await verifyContext.SagaStates.FindAsync(saga.SagaId);
        updated!.Status.Should().Be(SagaStatus.Failed);
        updated.ErrorMessage.Should().Be("Test error occurred");
    }

    [Fact]
    public async Task GetActiveSagasAsync_ShouldReturnRunningSagas()
    {
        // Arrange
        using var context = _fixture.CreateDbContext();
        var store = new SagaStoreEF(context);

        var running1 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "Step1",
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var running2 = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "Step2",
            Status = SagaStatus.Running,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        var completed = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            CurrentStep = "FinalStep",
            Status = SagaStatus.Completed,
            Data = "{}",
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            CompletedAtUtc = DateTime.UtcNow
        };

        context.SagaStates.AddRange(running1, running2, completed);
        await context.SaveChangesAsync();

        // Act
        var activeSagas = await store.GetActiveSagasAsync(batchSize: 10);

        // Assert
        var sagaList = activeSagas.ToList();
        sagaList.Should().HaveCount(2);
        sagaList.Should().Contain(s => s.SagaId == running1.SagaId);
        sagaList.Should().Contain(s => s.SagaId == running2.SagaId);
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
                CurrentStep = "Step1",
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
            stored.Should().NotBeNull();
        }
    }
}
