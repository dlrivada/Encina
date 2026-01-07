using Encina.ADO.Sqlite.Sagas;

namespace Encina.ADO.Sqlite.Tests.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaState"/>.
/// </summary>
public sealed class SagaStateTests
{
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
        var state = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":123}",
            Status = "Running",
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = lastUpdatedAt,
            CompletedAtUtc = completedAt,
            ErrorMessage = "Test error",
            CurrentStep = 2,
            TimeoutAtUtc = timeoutAt
        };

        // Assert
        state.SagaId.ShouldBe(sagaId);
        state.SagaType.ShouldBe("OrderSaga");
        state.Data.ShouldBe("{\"orderId\":123}");
        state.Status.ShouldBe("Running");
        state.StartedAtUtc.ShouldBe(startedAt);
        state.LastUpdatedAtUtc.ShouldBe(lastUpdatedAt);
        state.CompletedAtUtc.ShouldBe(completedAt);
        state.ErrorMessage.ShouldBe("Test error");
        state.CurrentStep.ShouldBe(2);
        state.TimeoutAtUtc.ShouldBe(timeoutAt);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Act
        var state = new SagaState();

        // Assert
        state.SagaId.ShouldBe(Guid.Empty);
        state.SagaType.ShouldBe(string.Empty);
        state.Data.ShouldBe(string.Empty);
        state.Status.ShouldBe(string.Empty);
        state.CompletedAtUtc.ShouldBeNull();
        state.ErrorMessage.ShouldBeNull();
        state.CurrentStep.ShouldBe(0);
        state.TimeoutAtUtc.ShouldBeNull();
    }

    [Fact]
    public void NullableProperties_CanBeSet()
    {
        // Arrange
        var state = new SagaState
        {
            CompletedAtUtc = null,
            ErrorMessage = null,
            TimeoutAtUtc = null
        };

        // Assert
        state.CompletedAtUtc.ShouldBeNull();
        state.ErrorMessage.ShouldBeNull();
        state.TimeoutAtUtc.ShouldBeNull();
    }
}
