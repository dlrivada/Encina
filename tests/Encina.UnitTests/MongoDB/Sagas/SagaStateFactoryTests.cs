using Encina.MongoDB.Sagas;

namespace Encina.UnitTests.MongoDB.Sagas;

public sealed class SagaStateFactoryTests
{
    private readonly SagaStateFactory _factory = new();

    [Fact]
    public void Create_ReturnsSagaStateWithCorrectProperties()
    {
        var sagaId = Guid.NewGuid();
        var sagaType = "OrderSaga";
        var data = """{"orderId": 123}""";
        var status = "Started";
        var currentStep = 0;
        var startedAtUtc = DateTime.UtcNow;

        var result = _factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc);

        result.ShouldNotBeNull();
        result.SagaId.ShouldBe(sagaId);
        result.SagaType.ShouldBe(sagaType);
        result.Data.ShouldBe(data);
        result.Status.ShouldBe(status);
        result.CurrentStep.ShouldBe(currentStep);
        result.StartedAtUtc.ShouldBe(startedAtUtc);
        result.LastUpdatedAtUtc.ShouldBe(startedAtUtc);
        result.TimeoutAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Create_WithTimeout_SetsTimeoutProperty()
    {
        var timeoutAtUtc = DateTime.UtcNow.AddHours(1);

        var result = _factory.Create(
            Guid.NewGuid(), "Type", "{}", "Started", 0, DateTime.UtcNow, timeoutAtUtc);

        result.TimeoutAtUtc.ShouldBe(timeoutAtUtc);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        var result = _factory.Create(Guid.NewGuid(), "Type", "{}", "Started", 0, DateTime.UtcNow);

        result.ShouldBeOfType<SagaState>();
    }
}
