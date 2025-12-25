using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using FluentAssertions;
using Xunit;
using SagaStatusString = Encina.Messaging.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.Tests.Sagas;

public class SagaStateFactoryTests
{
    private readonly SagaStateFactory _factory;

    public SagaStateFactoryTests()
    {
        _factory = new SagaStateFactory();
    }

    [Fact]
    public void Create_WithoutTimeout_ShouldCreateStateWithNullTimeout()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{\"orderId\":\"123\"}";
        var status = SagaStatusString.Running;
        var currentStep = 0;
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = _factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc);

        // Assert
        state.Should().NotBeNull();
        state.SagaId.Should().Be(sagaId);
        state.SagaType.Should().Be(sagaType);
        state.Data.Should().Be(data);
        state.Status.Should().Be(status);
        state.CurrentStep.Should().Be(currentStep);
        state.StartedAtUtc.Should().Be(startedAtUtc);
        state.LastUpdatedAtUtc.Should().Be(startedAtUtc);
        state.TimeoutAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_WithTimeout_ShouldCreateStateWithTimeout()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{\"orderId\":\"456\"}";
        var status = SagaStatusString.Running;
        var currentStep = 0;
        var startedAtUtc = DateTime.UtcNow;
        var timeoutAtUtc = DateTime.UtcNow.AddHours(1);

        // Act
        var state = _factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc, timeoutAtUtc);

        // Assert
        state.Should().NotBeNull();
        state.SagaId.Should().Be(sagaId);
        state.SagaType.Should().Be(sagaType);
        state.Data.Should().Be(data);
        state.Status.Should().Be(status);
        state.CurrentStep.Should().Be(currentStep);
        state.StartedAtUtc.Should().Be(startedAtUtc);
        state.LastUpdatedAtUtc.Should().Be(startedAtUtc);
        state.TimeoutAtUtc.Should().Be(timeoutAtUtc);
    }

    [Fact]
    public void Create_WithExplicitNullTimeout_ShouldCreateStateWithNullTimeout()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{}";
        var status = SagaStatusString.Running;
        var currentStep = 1;
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = _factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc, timeoutAtUtc: null);

        // Assert
        state.TimeoutAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldReturnISagaStateImplementation()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = _factory.Create(sagaId, "TestSaga", "{}", SagaStatusString.Running, 0, startedAtUtc);

        // Assert
        state.Should().BeAssignableTo<ISagaState>();
        state.Should().BeOfType<SagaState>();
    }

    [Fact]
    public void Create_ShouldSetLastUpdatedAtUtcEqualToStartedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var state = _factory.Create(sagaId, "TestSaga", "{}", SagaStatusString.Running, 0, startedAtUtc);

        // Assert
        state.LastUpdatedAtUtc.Should().Be(startedAtUtc);
    }

    [Theory]
    [InlineData(SagaStatusString.Running)]
    [InlineData(SagaStatusString.Completed)]
    [InlineData(SagaStatusString.Compensating)]
    [InlineData(SagaStatusString.Compensated)]
    [InlineData(SagaStatusString.Failed)]
    [InlineData(SagaStatusString.TimedOut)]
    public void Create_ShouldHandleAllStatusValues(string status)
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = _factory.Create(sagaId, "TestSaga", "{}", status, 0, startedAtUtc);

        // Assert
        state.Status.Should().Be(status);
    }
}
