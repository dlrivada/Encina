using Encina.MongoDB.Sagas;

namespace Encina.UnitTests.MongoDB.Sagas;

public sealed class SagaStateTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var state = new SagaState();
        state.SagaId.ShouldBe(Guid.Empty);
        state.SagaType.ShouldBeEmpty();
        state.Data.ShouldBeEmpty();
        state.Status.ShouldBeEmpty();
        state.CurrentStep.ShouldBe(0);
        state.CompletedAtUtc.ShouldBeNull();
        state.ErrorMessage.ShouldBeNull();
        state.TimeoutAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Properties_CanBeSet()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var state = new SagaState
        {
            SagaId = id,
            SagaType = "OrderSaga",
            Data = "{\"orderId\":1}",
            Status = "Running",
            CurrentStep = 3,
            StartedAtUtc = now,
            CompletedAtUtc = null,
            ErrorMessage = null,
            LastUpdatedAtUtc = now,
            TimeoutAtUtc = now.AddMinutes(30)
        };

        state.SagaId.ShouldBe(id);
        state.SagaType.ShouldBe("OrderSaga");
        state.CurrentStep.ShouldBe(3);
        state.Status.ShouldBe("Running");
        state.TimeoutAtUtc.ShouldNotBeNull();
    }
}
