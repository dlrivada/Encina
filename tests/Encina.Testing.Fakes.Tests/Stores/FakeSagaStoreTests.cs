using Encina.Testing.Fakes.Models;
using Encina.Testing.Fakes.Stores;

namespace Encina.Testing.Fakes.Tests.Stores;

public sealed class FakeSagaStoreTests
{
    private readonly FakeSagaStore _sut = new();

    [Fact]
    public async Task AddAsync_StoresSaga()
    {
        // Arrange
        var saga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{\"orderId\": 123}",
            Status = "Running"
        };

        // Act
        await _sut.AddAsync(saga);

        // Assert
        _sut.Sagas.Should().HaveCount(1);
        _sut.AddedSagas.Should().HaveCount(1);
        _sut.GetSaga(saga.SagaId).Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_ReturnsStoredSaga()
    {
        // Arrange
        var saga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running"
        };
        await _sut.AddAsync(saga);

        // Act
        var result = await _sut.GetAsync(saga.SagaId);

        // Assert
        result.Should().NotBeNull();
        result!.SagaId.Should().Be(saga.SagaId);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullForNonExistent()
    {
        // Act
        var result = await _sut.GetAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesExistingSaga()
    {
        // Arrange
        var saga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running",
            CurrentStep = 0
        };
        await _sut.AddAsync(saga);

        // Act
        saga.Status = "Completed";
        saga.CurrentStep = 3;
        saga.CompletedAtUtc = DateTime.UtcNow;
        await _sut.UpdateAsync(saga);

        // Assert
        var updated = _sut.GetSaga(saga.SagaId);
        updated!.Status.Should().Be("Completed");
        updated.CurrentStep.Should().Be(3);
        updated.CompletedAtUtc.Should().NotBeNull();
        _sut.UpdatedSagas.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetStuckSagasAsync_ReturnsStuckSagas()
    {
        // Arrange
        var stuckSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running",
            LastUpdatedAtUtc = DateTime.UtcNow.AddHours(-2)
        };

        var activeSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running",
            LastUpdatedAtUtc = DateTime.UtcNow
        };

        await _sut.AddAsync(stuckSaga);
        await _sut.AddAsync(activeSaga);

        // Act
        var stuck = await _sut.GetStuckSagasAsync(olderThan: TimeSpan.FromHours(1), batchSize: 10);

        // Assert
        stuck.Should().HaveCount(1);
        stuck.First().SagaId.Should().Be(stuckSaga.SagaId);
    }

    [Fact]
    public async Task GetExpiredSagasAsync_ReturnsExpiredSagas()
    {
        // Arrange
        var expiredSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running",
            TimeoutAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        var activeSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running",
            TimeoutAtUtc = DateTime.UtcNow.AddHours(1)
        };

        await _sut.AddAsync(expiredSaga);
        await _sut.AddAsync(activeSaga);

        // Act
        var expired = await _sut.GetExpiredSagasAsync(batchSize: 10);

        // Assert
        expired.Should().HaveCount(1);
        expired.First().SagaId.Should().Be(expiredSaga.SagaId);
    }

    [Fact]
    public async Task GetSagasByStatus_ReturnsFilteredSagas()
    {
        // Arrange
        var runningSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running"
        };

        var completedSaga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Completed"
        };

        await _sut.AddAsync(runningSaga);
        await _sut.AddAsync(completedSaga);

        // Act
        var running = _sut.GetSagasByStatus("Running");
        var completed = _sut.GetSagasByStatus("Completed");

        // Assert
        running.Should().HaveCount(1);
        completed.Should().HaveCount(1);
    }

    [Fact]
    public async Task WasSagaStarted_ByTypeName_ReturnsTrue()
    {
        // Arrange
        var saga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "MyApp.OrderFulfillmentSaga",
            Data = "{}",
            Status = "Running"
        };
        await _sut.AddAsync(saga);

        // Act
        var wasStarted = _sut.WasSagaStarted("MyApp.OrderFulfillmentSaga");
        var wasNotStarted = _sut.WasSagaStarted("NonExistent");

        // Assert
        wasStarted.Should().BeTrue();
        wasNotStarted.Should().BeFalse();
    }

    [Fact]
    public async Task Clear_ResetsAllState()
    {
        // Arrange
        var saga = new FakeSagaState
        {
            SagaId = Guid.NewGuid(),
            SagaType = "OrderSaga",
            Data = "{}",
            Status = "Running"
        };
        await _sut.AddAsync(saga);
        await _sut.UpdateAsync(saga);
        await _sut.SaveChangesAsync();

        // Act
        _sut.Clear();

        // Assert
        _sut.Sagas.Should().BeEmpty();
        _sut.AddedSagas.Should().BeEmpty();
        _sut.UpdatedSagas.Should().BeEmpty();
        _sut.SaveChangesCallCount.Should().Be(0);
    }
}
