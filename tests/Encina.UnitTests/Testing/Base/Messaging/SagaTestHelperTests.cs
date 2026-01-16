using Encina.Testing;
using Encina.Messaging.Sagas;
using Encina.Testing.Messaging;

namespace Encina.UnitTests.Testing.Base.Messaging;

/// <summary>
/// Unit tests for <see cref="SagaTestHelper"/>.
/// </summary>
public sealed class SagaTestHelperTests : IDisposable
{
    private readonly SagaTestHelper _helper;

    public SagaTestHelperTests()
    {
        _helper = new SagaTestHelper(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero));
    }

    public void Dispose()
    {
        _helper.Dispose();
    }

    #region Given Tests

    [Fact]
    public void GivenNoSagas_ShouldClearStore()
    {
        // Act
        _helper.GivenNoSagas();

        // Assert
        _helper.Store.GetSagas().ShouldBeEmpty();
    }

    [Fact]
    public void GivenNewSaga_ShouldAddRunningSagaAtStepZero()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenNewSaga<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" });

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.Status.ShouldBe(SagaStatus.Running);
        saga.CurrentStep.ShouldBe(0);
    }

    [Fact]
    public void GivenRunningSaga_ShouldAddRunningSagaAtSpecifiedStep()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenRunningSaga<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" },
            currentStep: 2);

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.Status.ShouldBe(SagaStatus.Running);
        saga.CurrentStep.ShouldBe(2);
    }

    [Fact]
    public void GivenCompletedSaga_ShouldAddCompletedSaga()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenCompletedSaga<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" });

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.Status.ShouldBe(SagaStatus.Completed);
        saga.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void GivenCompensatingSaga_ShouldAddCompensatingSaga()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenCompensatingSaga<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" },
            failedAtStep: 3,
            errorMessage: "Payment failed");

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.Status.ShouldBe(SagaStatus.Compensating);
        saga.CurrentStep.ShouldBe(3);
        saga.ErrorMessage.ShouldBe("Payment failed");
    }

    [Fact]
    public void GivenFailedSaga_ShouldAddFailedSaga()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenFailedSaga<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" },
            errorMessage: "Unrecoverable error");

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.Status.ShouldBe(SagaStatus.Failed);
        saga.ErrorMessage.ShouldBe("Unrecoverable error");
    }

    [Fact]
    public void GivenSagaWithTimeout_ShouldAddSagaWithTimeout()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper.GivenSagaWithTimeout<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" },
            timeoutIn: TimeSpan.FromMinutes(30));

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga.ShouldNotBeNull();
        saga.TimeoutAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region When Tests

    [Fact]
    public void WhenSagaStarts_ShouldAddNewSaga()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenNoSagas()
            .WhenSagaStarts<OrderSaga, OrderSagaData>(
                sagaId,
                new OrderSagaData { OrderId = "ORD-123" });

        // Assert
        _helper.Store.GetAddedSagas().Count.ShouldBe(1);
    }

    [Fact]
    public void WhenSagaAdvancesToNextStep_ShouldIncrementStep()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(
                sagaId,
                new OrderSagaData { OrderId = "ORD-123" },
                currentStep: 1)
            .WhenSagaAdvancesToNextStep(sagaId);

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga!.CurrentStep.ShouldBe(2);
    }

    [Fact]
    public void WhenSagaDataUpdated_ShouldUpdateData()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(
                sagaId,
                new OrderSagaData { OrderId = "ORD-123", PaymentId = null })
            .WhenSagaDataUpdated(sagaId, new OrderSagaData { OrderId = "ORD-123", PaymentId = "PAY-456" });

        // Assert
        var data = _helper.GetSagaData<OrderSagaData>(sagaId);
        data.PaymentId.ShouldBe("PAY-456");
    }

    [Fact]
    public void WhenSagaCompletes_ShouldSetStatusToCompleted()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
            .WhenSagaCompletes(sagaId);

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga!.Status.ShouldBe(SagaStatus.Completed);
        saga.CompletedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void WhenSagaStartsCompensating_ShouldSetStatusToCompensating()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
            .WhenSagaStartsCompensating(sagaId, "Step 2 failed");

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga!.Status.ShouldBe(SagaStatus.Compensating);
        saga.ErrorMessage.ShouldBe("Step 2 failed");
    }

    [Fact]
    public void WhenSagaFails_ShouldSetStatusToFailed()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
            .WhenSagaFails(sagaId, "Unrecoverable");

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga!.Status.ShouldBe(SagaStatus.Failed);
    }

    [Fact]
    public void WhenSagaTimesOut_ShouldSetStatusToTimedOut()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
            .WhenSagaTimesOut(sagaId);

        // Assert
        var saga = _helper.Store.GetSaga(sagaId);
        saga!.Status.ShouldBe(SagaStatus.TimedOut);
    }

    #endregion

    #region Then Tests

    [Fact]
    public void ThenSagaStatus_WhenCorrect_ShouldPass()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaCompletes(sagaId)
                .ThenSagaStatus(sagaId, SagaStatus.Completed));
    }

    [Fact]
    public void ThenSagaStatus_WhenIncorrect_ShouldThrow()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .When(_ => { }) // No state change
                .ThenSagaStatus(sagaId, SagaStatus.Completed));
    }

    [Fact]
    public void ThenSagaIsRunning_ShouldVerifyRunningStatus()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoSagas()
                .WhenSagaStarts<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .ThenSagaIsRunning(sagaId));
    }

    [Fact]
    public void ThenSagaIsCompleted_ShouldVerifyCompletedStatus()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaCompletes(sagaId)
                .ThenSagaIsCompleted(sagaId));
    }

    [Fact]
    public void ThenSagaIsCompensating_ShouldVerifyCompensatingStatus()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaStartsCompensating(sagaId)
                .ThenSagaIsCompensating(sagaId));
    }

    [Fact]
    public void ThenSagaHasFailed_ShouldVerifyFailedStatus()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaFails(sagaId)
                .ThenSagaHasFailed(sagaId));
    }

    [Fact]
    public void ThenSagaHasTimedOut_ShouldVerifyTimedOutStatus()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaTimesOut(sagaId)
                .ThenSagaHasTimedOut(sagaId));
    }

    [Fact]
    public void ThenSagaIsAtStep_WhenCorrect_ShouldPass()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" }, currentStep: 2)
                .WhenSagaAdvancesToNextStep(sagaId)
                .ThenSagaIsAtStep(sagaId, 3));
    }

    [Fact]
    public void ThenSagaData_WhenPredicateMatches_ShouldPass()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(
                    sagaId,
                    new OrderSagaData { OrderId = "ORD-123", PaymentId = "PAY-456" })
                .When(_ => { })
                .ThenSagaData<OrderSagaData>(sagaId, d => d.OrderId == "ORD-123" && d.PaymentId == "PAY-456"));
    }

    [Fact]
    public void ThenSagaWasStarted_ShouldVerifySagaCreation()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoSagas()
                .WhenSagaStarts<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .ThenSagaWasStarted<OrderSaga>());
    }

    [Fact]
    public void ThenSagaHasCompletedAt_ShouldVerifyTimestamp()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaCompletes(sagaId)
                .ThenSagaHasCompletedAt(sagaId));
    }

    [Fact]
    public void ThenSagaHasError_ShouldVerifyErrorMessage()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .WhenSagaFails(sagaId, "Payment gateway error")
                .ThenSagaHasError(sagaId, "Payment"));
    }

    [Fact]
    public void GetSagaData_ShouldReturnDeserializedData()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act
        _helper
            .GivenRunningSaga<OrderSaga, OrderSagaData>(
                sagaId,
                new OrderSagaData { OrderId = "ORD-123", PaymentId = "PAY-456" })
            .When(_ => { });

        var data = _helper.GetSagaData<OrderSagaData>(sagaId);

        // Assert
        data.OrderId.ShouldBe("ORD-123");
        data.PaymentId.ShouldBe("PAY-456");
    }

    #endregion

    #region Time Control Tests

    [Fact]
    public void AdvanceTimeBy_ShouldAdvanceTime()
    {
        // Arrange
        var startTime = _helper.TimeProvider.GetUtcNow();

        // Act
        _helper.AdvanceTimeBy(TimeSpan.FromHours(2));

        // Assert
        _helper.TimeProvider.GetUtcNow().ShouldBe(startTime.AddHours(2));
    }

    [Fact]
    public void AdvanceTimePastTimeout_ShouldAdvancePastSagaTimeout()
    {
        // Arrange
        var sagaId = Guid.NewGuid();
        _helper.GivenSagaWithTimeout<OrderSaga, OrderSagaData>(
            sagaId,
            new OrderSagaData { OrderId = "ORD-123" },
            timeoutIn: TimeSpan.FromMinutes(30));

        var saga = _helper.Store.GetSaga(sagaId);
        var timeoutAt = saga!.TimeoutAtUtc!.Value;

        // Act
        _helper.AdvanceTimePastTimeout(sagaId);

        // Assert
        _helper.TimeProvider.GetUtcNow().UtcDateTime.ShouldBeGreaterThan(timeoutAt);
    }

    #endregion

    #region Flow Validation Tests

    [Fact]
    public void ThenAssertions_BeforeWhen_ShouldThrow()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            _helper
                .GivenRunningSaga<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .ThenSagaIsRunning(sagaId));
    }

    [Fact]
    public void FluentChaining_ShouldWork()
    {
        // Arrange
        var sagaId = Guid.NewGuid();

        // Act & Assert
        Should.NotThrow(() =>
            _helper
                .GivenNoSagas()
                .WhenSagaStarts<OrderSaga, OrderSagaData>(sagaId, new OrderSagaData { OrderId = "ORD-123" })
                .ThenNoException()
                .ThenSagaWasStarted<OrderSaga>()
                .ThenSagaIsRunning(sagaId)
                .ThenSagaIsAtStep(sagaId, 0));
    }

    #endregion

    #region Test Types

    private sealed class OrderSaga { }

    private sealed class OrderSagaData
    {
        public string OrderId { get; init; } = string.Empty;
        public string? PaymentId { get; init; }
    }

    #endregion
}
