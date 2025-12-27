using Encina.AzureFunctions.Durable;
using FluentAssertions;
using Xunit;

namespace Encina.AzureFunctions.Tests.Durable;

public class DurableSagaBuilderTests
{
    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        // Act
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Assert
        builder.Should().NotBeNull();
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
        saga.Steps.Should().HaveCount(1);
        saga.Steps[0].StepName.Should().Be("Step1");
        saga.Steps[0].ExecuteActivityName.Should().Be("ExecuteStep1");
        saga.Steps[0].CompensateActivityName.Should().BeNull();
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
        saga.Steps.Should().HaveCount(3);
        saga.Steps[0].StepName.Should().Be("Step1");
        saga.Steps[1].StepName.Should().Be("Step2");
        saga.Steps[2].StepName.Should().Be("Step3");
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
        saga.Steps[0].CompensateActivityName.Should().Be("UndoSomething");
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
        saga.Steps[0].RetryOptions.Should().NotBeNull();
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
        saga.Steps[0].SkipCompensationOnFailure.Should().BeTrue();
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
        saga.Steps[0].RetryOptions.Should().NotBeNull();
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
        saga.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithNoSteps_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var action = () => builder.Build();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one step*");
    }

    [Fact]
    public void Step_WithNoExecute_ThrowsInvalidOperationException()
    {
        // Arrange & Act
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have an Execute activity*");
    }

    [Fact]
    public void Step_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step(string.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Execute_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Execute(string.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compensate_WithEmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .Step("Step1")
            .Execute("Execute1")
            .Compensate(string.Empty);

        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithTimeout_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => DurableSagaBuilder.Create<TestSagaData>()
            .WithTimeout(TimeSpan.FromSeconds(-1));

        action.Should().Throw<ArgumentOutOfRangeException>();
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
        saga.Steps.Should().HaveCount(3);

        saga.Steps[0].StepName.Should().Be("ReserveInventory");
        saga.Steps[0].ExecuteActivityName.Should().Be("ReserveInventoryActivity");
        saga.Steps[0].CompensateActivityName.Should().Be("ReleaseInventoryActivity");

        saga.Steps[1].StepName.Should().Be("ProcessPayment");
        saga.Steps[1].RetryOptions.Should().NotBeNull();

        saga.Steps[2].StepName.Should().Be("ShipOrder");
        saga.Steps[2].SkipCompensationOnFailure.Should().BeTrue();
    }

    private sealed class TestSagaData
    {
        public string? Value { get; set; }
    }

    private sealed class OrderSagaData
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
    }
}
