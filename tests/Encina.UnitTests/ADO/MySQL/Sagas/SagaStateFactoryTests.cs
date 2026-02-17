using Encina.ADO.MySQL.Sagas;

namespace Encina.UnitTests.ADO.MySQL.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaStateFactory"/>.
/// </summary>
public sealed class SagaStateFactoryTests
{
    private readonly SagaStateFactory _factory = new();

    [Fact]
    public void Create_ReturnsSagaStateWithCorrectProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var sagaType = "OrderSaga";
        var data = """{"orderId": "123"}""";
        var status = "Running";
        var currentStep = 2;
        var startedAtUtc = DateTime.UtcNow;

        // Act
        var result = _factory.Create(sagaId, sagaType, data, status, currentStep, startedAtUtc);

        // Assert
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
    public void Create_WithTimeout_SetsTimeoutAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow;
        var timeoutAtUtc = DateTime.UtcNow.AddMinutes(30);

        // Act
        var result = _factory.Create(
            sagaId,
            "TestSaga",
            "{}",
            "Running",
            0,
            startedAtUtc,
            timeoutAtUtc);

        // Assert
        result.ShouldNotBeNull();
        result.TimeoutAtUtc.ShouldBe(timeoutAtUtc);
    }

    [Fact]
    public void Create_ReturnsConcreteType()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        var result = _factory.Create(sagaId, "Type", "{}", "Status", 0, DateTime.UtcNow);

        // Assert
        result.ShouldBeOfType<SagaState>();
    }
}
