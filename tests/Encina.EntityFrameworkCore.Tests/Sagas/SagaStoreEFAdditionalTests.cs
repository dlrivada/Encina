using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Shouldly;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

using EfSagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.Tests.Sagas;

/// <summary>
/// Additional unit tests for <see cref="SagaStoreEF"/> focusing on constructor validation
/// and error handling paths.
/// </summary>
public class SagaStoreEFAdditionalTests : IDisposable
{
    private readonly TestDbContext _dbContext;
    private readonly SagaStoreEF _store;

    public SagaStoreEFAdditionalTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestDbContext(options);
        _store = new SagaStoreEF(_dbContext);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDbContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new SagaStoreEF(null!));
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_NullSaga_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.AddAsync(null!));
    }

    [Fact]
    public async Task AddAsync_WrongSagaType_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongSaga = Substitute.For<ISagaState>();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _store.AddAsync(wrongSaga));
        ex.Message.ShouldContain("SagaStoreEF requires saga state of type SagaState");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_NullSaga_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() =>
            _store.UpdateAsync(null!));
    }

    [Fact]
    public async Task UpdateAsync_WrongSagaType_ThrowsInvalidOperationException()
    {
        // Arrange
        var wrongSaga = Substitute.For<ISagaState>();

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() =>
            _store.UpdateAsync(wrongSaga));
        ex.Message.ShouldContain("SagaStoreEF requires saga state of type SagaState");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesLastUpdatedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var initialTime = DateTime.UtcNow.AddMinutes(-30);
        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "TestSaga",
            Data = "{}",
            Status = EfSagaStatus.Running,
            StartedAtUtc = initialTime,
            LastUpdatedAtUtc = initialTime,
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(saga);
        await _dbContext.SaveChangesAsync();

        // Act
        var beforeUpdate = DateTime.UtcNow;
        await _store.UpdateAsync(saga);
        var afterUpdate = DateTime.UtcNow;

        // Assert
        saga.LastUpdatedAtUtc.ShouldBeGreaterThanOrEqualTo(beforeUpdate);
        saga.LastUpdatedAtUtc.ShouldBeLessThanOrEqualTo(afterUpdate);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_ReturnsNullForNonExistentSaga()
    {
        // Act
        var result = await _store.GetAsync(Guid.NewGuid());

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region GetStuckSagasAsync Tests

    [Fact]
    public async Task GetStuckSagasAsync_ReturnsEmptyWhenNoStuckSagas()
    {
        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStuckSagasAsync_OrdersByLastUpdatedAtUtc()
    {
        // Arrange
        var older = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OlderSaga",
            Data = "{}",
            Status = EfSagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-4),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-4),
            CurrentStep = 0
        };

        var newer = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "NewerSaga",
            Data = "{}",
            Status = EfSagaStatus.Running,
            StartedAtUtc = DateTime.UtcNow.AddHours(-3),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-3),
            CurrentStep = 0
        };

        // Add in reverse order to test ordering
        await _dbContext.SagaStates.AddRangeAsync(newer, older);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = (await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10)).ToList();

        // Assert
        stuck.Count.ShouldBe(2);
        stuck[0].SagaId.ShouldBe(older.SagaId);
        stuck[1].SagaId.ShouldBe(newer.SagaId);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ExcludesCompletedSagas()
    {
        // Arrange
        var completedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompletedSaga",
            Data = "{}",
            Status = EfSagaStatus.Completed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-4),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-4),
            CompletedAtUtc = DateTime.UtcNow.AddHours(-4),
            CurrentStep = 3
        };

        await _dbContext.SagaStates.AddAsync(completedSaga);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStuckSagasAsync_ExcludesCompensatedSagas()
    {
        // Arrange
        var compensatedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompensatedSaga",
            Data = "{}",
            Status = EfSagaStatus.Compensated,
            StartedAtUtc = DateTime.UtcNow.AddHours(-4),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-4),
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(compensatedSaga);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetStuckSagasAsync_ExcludesFailedSagas()
    {
        // Arrange
        var failedSaga = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "FailedSaga",
            Data = "{}",
            Status = EfSagaStatus.Failed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-4),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-4),
            ErrorMessage = "Failed to compensate",
            CurrentStep = 2
        };

        await _dbContext.SagaStates.AddAsync(failedSaga);
        await _dbContext.SaveChangesAsync();

        // Act
        var stuck = await _store.GetStuckSagasAsync(
            olderThan: TimeSpan.FromHours(1),
            batchSize: 10);

        // Assert
        stuck.ShouldBeEmpty();
    }

    #endregion

    #region GetExpiredSagasAsync Tests

    [Fact]
    public async Task GetExpiredSagasAsync_ReturnsEmptyWhenNoExpiredSagas()
    {
        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ExcludesFailedSagas()
    {
        // Arrange
        var failedExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "FailedExpiredSaga",
            Data = "{}",
            Status = EfSagaStatus.Failed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
            TimeoutAtUtc = DateTime.UtcNow.AddMinutes(-30),
            CurrentStep = 2
        };

        await _dbContext.SagaStates.AddAsync(failedExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ExcludesCompensatedSagas()
    {
        // Arrange
        var compensatedExpired = new SagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "CompensatedExpiredSaga",
            Data = "{}",
            Status = EfSagaStatus.Compensated,
            StartedAtUtc = DateTime.UtcNow.AddHours(-2),
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-1),
            TimeoutAtUtc = DateTime.UtcNow.AddMinutes(-30),
            CurrentStep = 0
        };

        await _dbContext.SagaStates.AddAsync(compensatedExpired);
        await _dbContext.SaveChangesAsync();

        // Act
        var expired = await _store.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.ShouldBeEmpty();
    }

    #endregion

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
