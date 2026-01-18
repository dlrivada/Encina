using Encina.ADO.SqlServer.Sagas;

namespace Encina.UnitTests.ADO.SqlServer.Sagas;

public sealed class SagaStateTests
{
    [Fact]
    public void Properties_SetAndGetCorrectly()
    {
        var sagaId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow;
        var lastUpdatedAt = DateTime.UtcNow.AddMinutes(5);

        var saga = new SagaState
        {
            SagaId = sagaId,
            SagaType = "OrderSaga",
            Data = """{"orderId": 123}""",
            Status = "InProgress",
            StartedAtUtc = startedAt,
            LastUpdatedAtUtc = lastUpdatedAt,
            CurrentStep = 2,
            TimeoutAtUtc = startedAt.AddHours(1)
        };

        saga.SagaId.ShouldBe(sagaId);
        saga.SagaType.ShouldBe("OrderSaga");
        saga.Data.ShouldBe("""{"orderId": 123}""");
        saga.Status.ShouldBe("InProgress");
        saga.StartedAtUtc.ShouldBe(startedAt);
        saga.LastUpdatedAtUtc.ShouldBe(lastUpdatedAt);
        saga.CurrentStep.ShouldBe(2);
        saga.TimeoutAtUtc.ShouldBe(startedAt.AddHours(1));
        saga.CompletedAtUtc.ShouldBeNull();
        saga.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void CompletedState_SetsPropertiesCorrectly()
    {
        var completedAt = DateTime.UtcNow;

        var saga = new SagaState
        {
            Status = "Completed",
            CompletedAtUtc = completedAt,
            ErrorMessage = null
        };

        saga.Status.ShouldBe("Completed");
        saga.CompletedAtUtc.ShouldBe(completedAt);
        saga.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void FailedState_SetsErrorMessage()
    {
        var saga = new SagaState
        {
            Status = "Failed",
            ErrorMessage = "Payment failed"
        };

        saga.Status.ShouldBe("Failed");
        saga.ErrorMessage.ShouldBe("Payment failed");
    }
}
