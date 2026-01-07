using Encina.Messaging.Sagas;
using Shouldly;

namespace Encina.Messaging.Tests.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaStateSnapshot{TSagaData}"/>.
/// </summary>
public sealed class SagaStateSnapshotTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        const string sagaType = "OrderSaga";
        var data = new TestSagaData { OrderId = 123 };
        const string status = SagaStatus.Running;
        const int currentStep = 2;
        var startedAtUtc = DateTime.UtcNow;
        var completedAtUtc = (DateTime?)null;
        const string? errorMessage = null;

        // Act
        var snapshot = new SagaStateSnapshot<TestSagaData>(
            sagaId,
            sagaType,
            data,
            status,
            currentStep,
            startedAtUtc,
            completedAtUtc,
            errorMessage);

        // Assert
        snapshot.SagaId.ShouldBe(sagaId);
        snapshot.SagaType.ShouldBe(sagaType);
        snapshot.Data.ShouldBe(data);
        snapshot.Status.ShouldBe(status);
        snapshot.CurrentStep.ShouldBe(currentStep);
        snapshot.StartedAtUtc.ShouldBe(startedAtUtc);
        snapshot.CompletedAtUtc.ShouldBeNull();
        snapshot.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void Completed_Snapshot_HasCompletedAtUtc()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        var completedAtUtc = DateTime.UtcNow;

        // Act
        var snapshot = new SagaStateSnapshot<TestSagaData>(
            sagaId,
            "OrderSaga",
            new TestSagaData { OrderId = 456 },
            SagaStatus.Completed,
            5,
            startedAtUtc,
            completedAtUtc,
            null);

        // Assert
        snapshot.CompletedAtUtc.ShouldBe(completedAtUtc);
        snapshot.Status.ShouldBe(SagaStatus.Completed);
    }

    [Fact]
    public void Failed_Snapshot_HasErrorMessage()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        const string errorMessage = "Step 3 failed: Payment declined";

        // Act
        var snapshot = new SagaStateSnapshot<TestSagaData>(
            sagaId,
            "PaymentSaga",
            new TestSagaData { OrderId = 789 },
            SagaStatus.Failed,
            3,
            DateTime.UtcNow.AddMinutes(-10),
            DateTime.UtcNow,
            errorMessage);

        // Assert
        snapshot.ErrorMessage.ShouldBe(errorMessage);
        snapshot.Status.ShouldBe(SagaStatus.Failed);
    }

    [Fact]
    public void WithDifferentDataType_WorksCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var stringData = "simple-data";

        // Act
        var snapshot = new SagaStateSnapshot<string>(
            sagaId,
            "StringSaga",
            stringData,
            SagaStatus.Running,
            1,
            DateTime.UtcNow,
            null,
            null);

        // Assert
        snapshot.Data.ShouldBe(stringData);
    }

    [Fact]
    public void RecordEquality_WorksCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        var startedAtUtc = new DateTime(2026, 1, 7, 12, 0, 0, DateTimeKind.Utc);
        var data = new TestSagaData { OrderId = 100 };

        var snapshot1 = new SagaStateSnapshot<TestSagaData>(
            sagaId, "Saga", data, SagaStatus.Running, 1, startedAtUtc, null, null);

        var snapshot2 = new SagaStateSnapshot<TestSagaData>(
            sagaId, "Saga", data, SagaStatus.Running, 1, startedAtUtc, null, null);

        // Act & Assert
        snapshot1.ShouldBe(snapshot2);
    }

    [Fact]
    public void Deconstruction_WorksCorrectly()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        const string sagaType = "TestSaga";
        var data = new TestSagaData { OrderId = 42 };
        const string status = SagaStatus.Completed;
        const int step = 3;
        var started = DateTime.UtcNow.AddMinutes(-5);
        var completed = DateTime.UtcNow;
        const string error = "Some error";

        var snapshot = new SagaStateSnapshot<TestSagaData>(
            sagaId, sagaType, data, status, step, started, completed, error);

        // Act
        var (id, type, d, s, currentStep, startedAt, completedAt, err) = snapshot;

        // Assert
        id.ShouldBe(sagaId);
        type.ShouldBe(sagaType);
        d.ShouldBe(data);
        s.ShouldBe(status);
        currentStep.ShouldBe(step);
        startedAt.ShouldBe(started);
        completedAt.ShouldBe(completed);
        err.ShouldBe(error);
    }

    private sealed class TestSagaData
    {
        public int OrderId { get; set; }
    }
}
