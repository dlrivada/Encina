using Encina.EntityFrameworkCore.Sagas;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.MySQL.Sagas;

/// <summary>
/// MySQL-specific integration tests for <see cref="SagaStoreEF"/>.
/// Uses real MySQL database via Testcontainers.
/// Tests are skipped until Pomelo.EntityFrameworkCore.MySql v10 is released.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "MySQL")]
[Collection("EFCore-MySQL")]
public sealed class SagaStoreEFMySqlTests : IAsyncLifetime
{
    private readonly EFCoreMySqlFixture _fixture;

    public SagaStoreEFMySqlTests(EFCoreMySqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [SkippableFact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistSaga()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new SagaStoreEF(context);

        var saga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "TestSaga",
            Data = "{\"test\":\"data\"}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        await store.AddAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        var stored = await verifyContext.Set<SagaState>().FindAsync(saga.SagaId);
        stored.ShouldNotBeNull();
        stored!.SagaType.ShouldBe("TestSaga");
        stored.Status.ShouldBe(SagaStatus.Running);
    }

    [SkippableFact]
    public async Task GetAsync_ExistingSaga_ShouldReturnSaga()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new SagaStoreEF(context);

        var sagaId = Guid.NewGuid();
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        context.Set<SagaState>().Add(saga);
        await context.SaveChangesAsync();

        // Act
        var result = await store.GetAsync(sagaId);

        // Assert
        result.ShouldNotBeNull();
        result!.SagaId.ShouldBe(sagaId);
    }

    [SkippableFact]
    public async Task GetStuckSagasAsync_ShouldReturnOldRunningSagas()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new SagaStoreEF(context);

        var stuckSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "StuckSaga",
            Data = "{}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2) // Old
        };

        var activeSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ActiveSaga",
            Data = "{}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            LastUpdatedAtUtc = DateTime.UtcNow.AddMinutes(-5) // Recent
        };

        context.Set<SagaState>().AddRange(stuckSaga, activeSaga);
        await context.SaveChangesAsync();

        // Act
        var stuckSagas = await store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        var sagaList = stuckSagas.ToList();
        sagaList.Count.ShouldBe(1);
        sagaList.ShouldContain(s => s.SagaId == stuckSaga.SagaId);
    }

    [SkippableFact]
    public async Task ConcurrentWrites_ShouldNotCorruptData()
    {
        Skip.If(true, "MySQL support requires Pomelo.EntityFrameworkCore.MySql v10.0.0 for EF Core 10 compatibility");

        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await using var ctx = _fixture.CreateDbContext<TestEFDbContext>();
            var store = new SagaStoreEF(ctx);

            var saga = new SagaState
            {
                SagaId = Guid.NewGuid(),
                SagaType = $"ConcurrentSaga{i}",
                Data = $"{{\"index\":{i}}}",
                CurrentStep = 1,
                Status = SagaStatus.Running,
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
        await using var verifyContext = _fixture.CreateDbContext<TestEFDbContext>();
        foreach (var id in sagaIds)
        {
            var stored = await verifyContext.Set<SagaState>().FindAsync(id);
            stored.ShouldNotBeNull();
        }
    }
}
