using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Shouldly;
using Xunit;
using SagaStatusString = Encina.Messaging.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.Tests.Sagas;

public class SagaStateFactoryTests
{
    [Fact]
    public void Create_WithoutTimeout_ShouldCreateStateWithNullTimeout()
    {
        // Arrange
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{\"orderId\":\"123\"}";
        var status = SagaStatusString.Running;
        var currentStep = 0;
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc);

        // Assert
        state.ShouldNotBeNull();
        state.SagaId.ShouldBe(sagaId);
        state.SagaType.ShouldBe(sagaType);
        state.Data.ShouldBe(data);
        state.Status.ShouldBe(status);
        state.CurrentStep.ShouldBe(currentStep);
        state.StartedAtUtc.ShouldBe(startedAtUtc);
        state.LastUpdatedAtUtc.ShouldBe(startedAtUtc);
        state.TimeoutAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_WithTimeout_ShouldCreateStateWithTimeout()
    {
        // Arrange
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{\"orderId\":\"456\"}";
        var status = SagaStatusString.Running;
        var currentStep = 0;
        var startedAtUtc = DateTime.UtcNow;
        var timeoutAtUtc = DateTime.UtcNow.AddHours(1);

        // Act
        var state = factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc, timeoutAtUtc);

        // Assert
        state.ShouldNotBeNull();
        state.SagaId.ShouldBe(sagaId);
        state.SagaType.ShouldBe(sagaType);
        state.Data.ShouldBe(data);
        state.Status.ShouldBe(status);
        state.CurrentStep.ShouldBe(currentStep);
        state.StartedAtUtc.ShouldBe(startedAtUtc);
        state.LastUpdatedAtUtc.ShouldBe(startedAtUtc);
        state.TimeoutAtUtc.ShouldBe(timeoutAtUtc);
    }

    [Fact]
    public void Create_WithExplicitNullTimeout_ShouldCreateStateWithNullTimeout()
    {
        // Arrange
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var sagaType = "TestSaga";
        var data = "{}";
        var status = SagaStatusString.Running;
        var currentStep = 1;
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc, timeoutAtUtc: null);

        // Assert
        state.TimeoutAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldReturnISagaStateImplementation()
    {
        // Arrange
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = factory.Create(sagaId, "TestSaga", "{}", SagaStatusString.Running, 0, startedAtUtc);

        // Assert
        state.ShouldBeAssignableTo<ISagaState>();
        state.ShouldBeOfType<SagaState>();
    }

    [Fact]
    public void Create_ShouldSetLastUpdatedAtUtcEqualToStartedAtUtc()
    {
        // Arrange
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-5);

        // Act
        var state = factory.Create(sagaId, "TestSaga", "{}", SagaStatusString.Running, 0, startedAtUtc);

        // Assert
        state.LastUpdatedAtUtc.ShouldBe(startedAtUtc);
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
        var factory = new SagaStateFactory();
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var state = factory.Create(sagaId, "TestSaga", "{}", status, 0, startedAtUtc);

        // Assert
        state.Status.ShouldBe(status);
    }
}
