using Encina.EntityFrameworkCore.Sagas;
using Encina.Messaging.Sagas;
using Shouldly;
using Xunit;

using EfSagaStatus = Encina.EntityFrameworkCore.Sagas.SagaStatus;

namespace Encina.EntityFrameworkCore.Tests.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaState"/>.
/// </summary>
public sealed class SagaStateTests
{
    #region Status Property Tests

    [Theory]
    [InlineData(EfSagaStatus.Running, "Running")]
    [InlineData(EfSagaStatus.Completed, "Completed")]
    [InlineData(EfSagaStatus.Compensating, "Compensating")]
    [InlineData(EfSagaStatus.Compensated, "Compensated")]
    [InlineData(EfSagaStatus.Failed, "Failed")]
    [InlineData(EfSagaStatus.TimedOut, "TimedOut")]
    public void Status_GetAsString_ReturnsCorrectValue(EfSagaStatus status, string expectedString)
    {
        // Arrange
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}",
            Status = status
        };

        // Act
        var stringStatus = ((ISagaState)sagaState).Status;

        // Assert
        stringStatus.ShouldBe(expectedString);
    }

    [Theory]
    [InlineData("Running", EfSagaStatus.Running)]
    [InlineData("Completed", EfSagaStatus.Completed)]
    [InlineData("Compensating", EfSagaStatus.Compensating)]
    [InlineData("Compensated", EfSagaStatus.Compensated)]
    [InlineData("Failed", EfSagaStatus.Failed)]
    [InlineData("TimedOut", EfSagaStatus.TimedOut)]
    public void Status_SetFromString_SetsCorrectValue(string stringStatus, EfSagaStatus expectedStatus)
    {
        // Arrange
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}"
        };

        // Act
        ((ISagaState)sagaState).Status = stringStatus;

        // Assert
        sagaState.Status.ShouldBe(expectedStatus);
    }

    [Fact]
    public void Status_SetInvalidString_ThrowsArgumentException()
    {
        // Arrange
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}"
        };

        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            ((ISagaState)sagaState).Status = "InvalidStatus");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var lastUpdatedAt = DateTime.UtcNow.AddMinutes(5);
        var completedAt = DateTime.UtcNow.AddMinutes(10);
        var timeoutAt = DateTime.UtcNow.AddHours(1);

        // Act
        var sagaState = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderProcessingSaga",
            Data = "{\"orderId\":123}",
            CurrentStep = 2,
            Status = EfSagaStatus.Running,
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = lastUpdatedAt,
            CompletedAtUtc = completedAt,
            ErrorMessage = null,
            CorrelationId = "corr-123",
            TimeoutAtUtc = timeoutAt,
            Metadata = "{\"user\":\"admin\"}"
        };

        // Assert
        sagaState.SagaId.ShouldBe(sagaId);
        sagaState.SagaType.ShouldBe("OrderProcessingSaga");
        sagaState.Data.ShouldBe("{\"orderId\":123}");
        sagaState.CurrentStep.ShouldBe(2);
        sagaState.Status.ShouldBe(EfSagaStatus.Running);
        sagaState.StartedAtUtc.ShouldBe(startedAt);
        sagaState.LastUpdatedAtUtc.ShouldBe(lastUpdatedAt);
        sagaState.CompletedAtUtc.ShouldBe(completedAt);
        sagaState.ErrorMessage.ShouldBeNull();
        sagaState.CorrelationId.ShouldBe("corr-123");
        sagaState.TimeoutAtUtc.ShouldBe(timeoutAt);
        sagaState.Metadata.ShouldBe("{\"user\":\"admin\"}");
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange & Act
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}",
            CompletedAtUtc = null,
            ErrorMessage = null,
            CorrelationId = null,
            TimeoutAtUtc = null,
            Metadata = null
        };

        // Assert
        sagaState.CompletedAtUtc.ShouldBeNull();
        sagaState.ErrorMessage.ShouldBeNull();
        sagaState.CorrelationId.ShouldBeNull();
        sagaState.TimeoutAtUtc.ShouldBeNull();
        sagaState.Metadata.ShouldBeNull();
    }

    [Fact]
    public void DefaultCurrentStep_IsZero()
    {
        // Arrange & Act
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}"
        };

        // Assert
        sagaState.CurrentStep.ShouldBe(0);
    }

    [Fact]
    public void DefaultStatus_IsRunning()
    {
        // Arrange & Act
        var sagaState = new SagaState
        {
            SagaType = "TestSaga",
            Data = "{}"
        };

        // Assert
        sagaState.Status.ShouldBe(EfSagaStatus.Running);
    }

    #endregion

    #region SagaStatus Enum Tests

    [Fact]
    public void SagaStatus_AllValuesAreDefined()
    {
        // Assert
        Enum.GetValues<EfSagaStatus>().Length.ShouldBe(6);
        ((int)EfSagaStatus.Running).ShouldBe(0);
        ((int)EfSagaStatus.Completed).ShouldBe(1);
        ((int)EfSagaStatus.Compensating).ShouldBe(2);
        ((int)EfSagaStatus.Compensated).ShouldBe(3);
        ((int)EfSagaStatus.Failed).ShouldBe(4);
        ((int)EfSagaStatus.TimedOut).ShouldBe(5);
    }

    #endregion
}
