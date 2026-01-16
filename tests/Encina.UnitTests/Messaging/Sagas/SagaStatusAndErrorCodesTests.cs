using Encina.Messaging.Sagas;
using Shouldly;

namespace Encina.UnitTests.Messaging.Sagas;

/// <summary>
/// Unit tests for <see cref="SagaStatus"/> and <see cref="SagaErrorCodes"/>.
/// </summary>
public sealed class SagaStatusAndErrorCodesTests
{
    #region SagaStatus Tests

    [Fact]
    public void SagaStatus_Running_HasCorrectValue()
    {
        SagaStatus.Running.ShouldBe("Running");
    }

    [Fact]
    public void SagaStatus_Completed_HasCorrectValue()
    {
        SagaStatus.Completed.ShouldBe("Completed");
    }

    [Fact]
    public void SagaStatus_Compensating_HasCorrectValue()
    {
        SagaStatus.Compensating.ShouldBe("Compensating");
    }

    [Fact]
    public void SagaStatus_Compensated_HasCorrectValue()
    {
        SagaStatus.Compensated.ShouldBe("Compensated");
    }

    [Fact]
    public void SagaStatus_Failed_HasCorrectValue()
    {
        SagaStatus.Failed.ShouldBe("Failed");
    }

    [Fact]
    public void SagaStatus_TimedOut_HasCorrectValue()
    {
        SagaStatus.TimedOut.ShouldBe("TimedOut");
    }

    #endregion

    #region SagaErrorCodes Tests

    [Fact]
    public void SagaErrorCodes_NotFound_HasCorrectValue()
    {
        SagaErrorCodes.NotFound.ShouldBe("saga.not_found");
    }

    [Fact]
    public void SagaErrorCodes_InvalidStatus_HasCorrectValue()
    {
        SagaErrorCodes.InvalidStatus.ShouldBe("saga.invalid_status");
    }

    [Fact]
    public void SagaErrorCodes_DeserializationFailed_HasCorrectValue()
    {
        SagaErrorCodes.DeserializationFailed.ShouldBe("saga.deserialization_failed");
    }

    [Fact]
    public void SagaErrorCodes_StepFailed_HasCorrectValue()
    {
        SagaErrorCodes.StepFailed.ShouldBe("saga.step_failed");
    }

    [Fact]
    public void SagaErrorCodes_CompensationFailed_HasCorrectValue()
    {
        SagaErrorCodes.CompensationFailed.ShouldBe("saga.compensation_failed");
    }

    [Fact]
    public void SagaErrorCodes_Timeout_HasCorrectValue()
    {
        SagaErrorCodes.Timeout.ShouldBe("saga.timeout");
    }

    [Fact]
    public void SagaErrorCodes_HandlerCancelled_HasCorrectValue()
    {
        SagaErrorCodes.HandlerCancelled.ShouldBe("saga.handler.cancelled");
    }

    [Fact]
    public void SagaErrorCodes_HandlerFailed_HasCorrectValue()
    {
        SagaErrorCodes.HandlerFailed.ShouldBe("saga.handler.failed");
    }

    #endregion
}
