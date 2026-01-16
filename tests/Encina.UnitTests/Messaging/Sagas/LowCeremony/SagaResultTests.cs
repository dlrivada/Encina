using Encina.Messaging.Sagas.LowCeremony;
using Shouldly;

namespace Encina.UnitTests.Messaging.Sagas.LowCeremony;

/// <summary>
/// Unit tests for <see cref="SagaResult{TData}"/>.
/// </summary>
public sealed class SagaResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var data = new TestSagaData { Value = "test" };
        const int stepsExecuted = 5;

        // Act
        var result = new SagaResult<TestSagaData>(sagaId, data, stepsExecuted);

        // Assert
        result.SagaId.ShouldBe(sagaId);
        result.Data.ShouldBe(data);
        result.StepsExecuted.ShouldBe(stepsExecuted);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var data = new TestSagaData { Value = "test" };
        const int stepsExecuted = 3;

        var result = new SagaResult<TestSagaData>(sagaId, data, stepsExecuted);

        // Act
        var (id, d, steps) = result;

        // Assert
        id.ShouldBe(sagaId);
        d.ShouldBe(data);
        steps.ShouldBe(stepsExecuted);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var data = new TestSagaData { Value = "test" };

        var result1 = new SagaResult<TestSagaData>(sagaId, data, 2);
        var result2 = new SagaResult<TestSagaData>(sagaId, data, 2);

        // Assert
        result1.ShouldBe(result2);
    }

    [Fact]
    public void ZeroStepsExecuted_IsValid()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var data = new TestSagaData { Value = "empty" };

        // Act
        var result = new SagaResult<TestSagaData>(sagaId, data, 0);

        // Assert
        result.StepsExecuted.ShouldBe(0);
    }

    private sealed class TestSagaData
    {
        public string Value { get; set; } = string.Empty;
    }
}
