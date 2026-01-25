using Encina.EntityFrameworkCore.Sagas;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.IntegrationTests.Infrastructure.EntityFrameworkCore.SqlServer.Sagas;

/// <summary>
/// SQL Server-specific integration tests for <see cref="SagaStoreEF"/>.
/// Uses real SQL Server database via Testcontainers.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
[Collection("EFCore-SqlServer")]
public sealed class SagaStoreEFSqlServerTests : IAsyncLifetime
{
    private readonly EFCoreSqlServerFixture _fixture;

    public SagaStoreEFSqlServerTests(EFCoreSqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _fixture.ClearAllDataAsync();
    }

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistSaga()
    {
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

    [Fact]
    public async Task GetAsync_ExistingSaga_ShouldReturnSaga()
    {
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

    [Fact]
    public async Task GetStuckSagasAsync_ShouldReturnOldRunningSagas()
    {
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

        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            CurrentStep = 3,
            Status = SagaStatus.Completed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        context.Set<SagaState>().AddRange(stuckSaga, activeSaga, completedSaga);
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

    [Fact]
    public async Task GetExpiredSagasAsync_ShouldReturnExpiredSagas()
    {
        // Arrange
        await using var context = _fixture.CreateDbContext<TestEFDbContext>();
        await context.Database.EnsureCreatedAsync();
        var store = new SagaStoreEF(context);

        var expiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "ExpiredSaga",
            Data = "{}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddDays(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddDays(-1),
            TimeoutAtUtc = DateTime.UtcNow.AddDays(-1) // Expired
        };

        var notExpiredSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NotExpiredSaga",
            Data = "{}",
            CurrentStep = 1,
            Status = SagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow,
            LastUpdatedAtUtc = DateTime.UtcNow,
            TimeoutAtUtc = DateTime.UtcNow.AddDays(1) // Not expired
        };

        context.Set<SagaState>().AddRange(expiredSaga, notExpiredSaga);
        await context.SaveChangesAsync();

        // Act
        var expiredSagas = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        var sagaList = expiredSagas.ToList();
        sagaList.Count.ShouldBe(1);
        sagaList.ShouldContain(s => s.SagaId == expiredSaga.SagaId);
    }
}
