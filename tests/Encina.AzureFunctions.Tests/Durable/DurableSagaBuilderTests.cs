using Encina.AzureFunctions.Durable;
using Encina.AzureFunctions.Tests.Fakers;
using Shouldly;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class DurableSagaBuilderTests
{
    #region DurableSagaError Tests

    [Fact]
    public void DurableSagaError_CanBeCreated()
    {
        // Arrange & Act
        var error = new DurableSagaError
        {
            FailedStep = "ProcessPayment",
            OriginalError = EncinaErrors.Create("payment.failed", "Payment processing failed"),
            CompensationErrors = new Dictionary<string, EncinaError?>
            {
                ["ReserveInventory"] = null
            },
            WasCompensated = true
        };

        // Assert
        error.FailedStep.ShouldBe("ProcessPayment");
        error.OriginalError.Message.ShouldContain("Payment processing failed");
        error.CompensationErrors.ShouldContainKey("ReserveInventory");
        error.CompensationErrors["ReserveInventory"].ShouldBeNull();
        error.WasCompensated.ShouldBeTrue();
    }

    [Fact]
    public void DurableSagaError_WithPartialCompensation_CanTrackFailedCompensations()
    {
        // Arrange
        var compensationError = EncinaErrors.Create("inventory.release_failed", "Could not release inventory");

        // Act
        var error = new DurableSagaError
        {
            FailedStep = "ShipOrder",
            OriginalError = EncinaErrors.Create("shipping.failed", "Shipping failed"),
            CompensationErrors = new Dictionary<string, EncinaError?>
            {
                ["ProcessPayment"] = null, // Refund succeeded
                ["ReserveInventory"] = compensationError // Release failed
            },
            WasCompensated = false
        };

        // Assert
        error.WasCompensated.ShouldBeFalse();
        error.CompensationErrors["ProcessPayment"].ShouldBeNull();
        var inventoryError = error.CompensationErrors["ReserveInventory"];
        inventoryError.ShouldNotBeNull();
        inventoryError.Value.Message.ShouldContain("Could not release inventory");
    }

    [Fact]
    public void DurableSagaError_WithNoCompensations_HasEmptyDictionary()
    {
        // Arrange & Act
        var error = new DurableSagaError
        {
            FailedStep = "FirstStep",
            OriginalError = EncinaErrors.Create("first.failed", "First step failed"),
            CompensationErrors = [],
            WasCompensated = true // No compensations to run
        };

        // Assert
        error.CompensationErrors.ShouldBeEmpty();
        error.WasCompensated.ShouldBeTrue();
    }

    #endregion

    #region DurableSagaBuilder.Create Tests

    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        // Act
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void Build_WithSingleStep_CreatesSagaWithOneStep()
    {
        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
                .Execute("ExecuteStep1")
            .Build();

        // Assert
        saga.Steps.Count.ShouldBe(1);
        saga.Steps[0].StepName.ShouldBe("Step1");
        saga.Steps[0].ExecuteActivityName.ShouldBe("ExecuteStep1");
        saga.Steps[0].CompensateActivityName.ShouldBeNull();
    }

    [Fact]
    public void Build_WithMultipleSteps_CreatesSagaWithAllSteps()
    {
        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
                .Execute("Execute1")
                .Compensate("Compensate1")
            .Step("Step2")
                .Execute("Execute2")
                .Compensate("Compensate2")
            .Step("Step3")
                .Execute("Execute3")
            .Build();

        // Assert
        saga.Steps.Count.ShouldBe(3);
        saga.Steps[0].StepName.ShouldBe("Step1");
        saga.Steps[1].StepName.ShouldBe("Step2");
        saga.Steps[2].StepName.ShouldBe("Step3");
    }

    [Fact]
    public void Step_WithCompensation_SetsCompensateActivityName()
    {
        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
                .Execute("DoSomething")
                .Compensate("UndoSomething")
            .Build();

        // Assert
        saga.Steps[0].CompensateActivityName.ShouldBe("UndoSomething");
    }

    [Fact]
    public void Step_WithRetry_SetsRetryOptions()
    {
        // Arrange
        var retryOptions = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 3,
            firstRetryInterval: TimeSpan.FromSeconds(5));

        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
                .Execute("Execute1")
                .WithRetry(retryOptions)
            .Build();

        // Assert
        saga.Steps[0].RetryOptions.ShouldNotBeNull();
    }

    [Fact]
    public void Step_WithSkipCompensationOnFailure_SetsFlag()
    {
        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
                .Execute("Execute1")
                .Compensate("Compensate1")
                .SkipCompensationOnFailure()
            .Build();

        // Assert
        saga.Steps[0].SkipCompensationOnFailure.ShouldBeTrue();
    }

    [Fact]
    public void WithDefaultRetryOptions_AppliesDefaultToAllSteps()
    {
        // Arrange
        var defaultRetryOptions = OrchestrationContextExtensions.CreateRetryOptions(
            maxRetries: 5,
            firstRetryInterval: TimeSpan.FromSeconds(10));

        // Act - Step without explicit retry gets default
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .WithDefaultRetryOptions(defaultRetryOptions)
            .Step("Step1")
                .Execute("Execute1")
            .Build();

        // Assert - default options are applied to steps without explicit retry at build time
        saga.Steps[0].RetryOptions.ShouldNotBeNull();
    }

    [Fact]
    public void WithTimeout_SetsSagaTimeout()
    {
        // Act
        var saga = DurableSagaBuilder.Create<TestSagaData>()
            .WithTimeout(TimeSpan.FromMinutes(30))
            .Step("Step1")
                .Execute("Execute1")
            .Build();

        // Assert
        saga.ShouldNotBeNull();
    }

    [Fact]
    public void Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var action = () => builder.Build();
        var ex = Should.Throw<InvalidOperationException>(action);
        ex.Message.ShouldContain("at least one step");
    }

    [Fact]
    public void Step_WithNoExecute_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Build();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(action);
        ex.Message.ShouldContain("must have an Execute activity");
    }

    [Fact]
    public void Step_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step(string.Empty);

        var ex = Should.Throw<ArgumentException>(action);
        ex.ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void Execute_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Execute(string.Empty);

        var ex = Should.Throw<ArgumentException>(action);
        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void Compensate_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Execute("Execute1")
            .Compensate(string.Empty);

        var ex = Should.Throw<ArgumentException>(action);
        ex.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void WithTimeout_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .WithTimeout(TimeSpan.FromSeconds(-1));

        var ex = Should.Throw<ArgumentOutOfRangeException>(action);
        ex.ParamName.ShouldBe("timeout");
    }

    [Fact]
    public void FluentChaining_CreatesComplexSaga()
    {
        // Act
        var saga = DurableSagaBuilder.Create<OrderSagaData>()
            .WithTimeout(TimeSpan.FromHours(1))
            .Step("ReserveInventory")
                .Execute("ReserveInventoryActivity")
                .Compensate("ReleaseInventoryActivity")
            .Step("ProcessPayment")
                .Execute("ProcessPaymentActivity")
                .Compensate("RefundPaymentActivity")
                .WithRetry(OrchestrationContextExtensions.CreateRetryOptions(3, TimeSpan.FromSeconds(5)))
            .Step("ShipOrder")
                .Execute("ShipOrderActivity")
                .Compensate("CancelShipmentActivity")
                .SkipCompensationOnFailure()
            .Build();

        // Assert
        saga.Steps.Count.ShouldBe(3);

        saga.Steps[0].StepName.ShouldBe("ReserveInventory");
        saga.Steps[0].ExecuteActivityName.ShouldBe("ReserveInventoryActivity");
        saga.Steps[0].CompensateActivityName.ShouldBe("ReleaseInventoryActivity");

        saga.Steps[1].StepName.ShouldBe("ProcessPayment");
        saga.Steps[1].RetryOptions.ShouldNotBeNull();

        saga.Steps[2].StepName.ShouldBe("ShipOrder");
        saga.Steps[2].SkipCompensationOnFailure.ShouldBeTrue();
    }

    #endregion
}
