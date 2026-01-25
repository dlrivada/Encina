using Encina.EntityFrameworkCore.Sagas;
using Encina.TestInfrastructure.Fixtures.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace Encina.TestInfrastructure.EFCore;

/// <summary>
/// Abstract base class for SagaStoreEF integration tests.
/// Contains test methods that run against any database provider.
/// </summary>
/// <typeparam name="TFixture">The type of EF Core fixture to use.</typeparam>
/// <typeparam name="TContext">The type of DbContext to use.</typeparam>
public abstract class SagaStoreEFTestsBase<TFixture, TContext> : EFCoreTestBase<TFixture>
    where TFixture : class, IEFCoreFixture
    where TContext : DbContext
{
    /// <summary>
    /// Gets the SagaStates DbSet from the context.
    /// </summary>
    protected abstract DbSet<SagaState> GetSagaStates(TContext context);

    [Fact]
    public async Task AddAsync_WithRealDatabase_ShouldPersistSaga()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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
        await using var verifyContext = CreateDbContext<TContext>();
        var stored = await GetSagaStates(verifyContext).FindAsync(saga.SagaId);
        stored.ShouldNotBeNull();
        stored!.SagaType.ShouldBe("TestSaga");
        stored.Status.ShouldBe(SagaStatus.Running);
    }

    [Fact]
    public async Task GetAsync_ExistingSaga_ShouldReturnSaga()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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

        GetSagaStates(context).Add(saga);
        await context.SaveChangesAsync();

        // Act
        var result = await store.GetAsync(sagaId);

        // Assert
        result.ShouldNotBeNull();
        result!.SagaId.ShouldBe(sagaId);
    }

    [Fact]
    public async Task GetAsync_NonExistingSaga_ShouldReturnNull()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
        var store = new SagaStoreEF(context);

        // Act
        var result = await store.GetAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldModifySaga()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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

        GetSagaStates(context).Add(saga);
        await context.SaveChangesAsync();

        // Modify saga
        saga.CurrentStep = 2;
        saga.Status = SagaStatus.Completed;
        saga.CompletedAtUtc = DateTime.UtcNow;

        // Act
        await store.UpdateAsync(saga);
        await store.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateDbContext<TContext>();
        var updated = await GetSagaStates(verifyContext).FindAsync(sagaId);
        updated!.CurrentStep.ShouldBe(2);
        updated.Status.ShouldBe(SagaStatus.Completed);
        updated.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetStuckSagasAsync_ShouldReturnOldRunningSagas()
    {
        // Arrange
        await using var context = CreateDbContext<TContext>();
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

        GetSagaStates(context).AddRange(stuckSaga, activeSaga, completedSaga);
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
        await using var context = CreateDbContext<TContext>();
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

        GetSagaStates(context).AddRange(expiredSaga, notExpiredSaga);
        await context.SaveChangesAsync();

        // Act
        var expiredSagas = await store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        var sagaList = expiredSagas.ToList();
        sagaList.Count.ShouldBe(1);
        sagaList.ShouldContain(s => s.SagaId == expiredSaga.SagaId);
    }
}
