using Encina.AzureFunctions.Durable;
using FluentAssertions;
using Microsoft.DurableTask;
using Xunit;

namespace Encina.AzureFunctions.ContractTests.Durable;

/// <summary>
/// Contract tests to verify that DurableSagaBuilder follows the saga pattern contract.
/// </summary>
[Trait("Category", "Contract")]
public sealed class DurableSagaBuilderContractTests
{
    [Fact]
    public void Contract_SagaBuilder_CanBeCreated()
    {
        // Act
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void Contract_SagaBuilder_CanAddStepWithExecute()
    {
        // Arrange & Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity")
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps.Should().HaveCount(1);
        saga.Steps[0].StepName.Should().Be("TestStep");
        saga.Steps[0].ExecuteActivityName.Should().Be("TestActivity");
    }

    [Fact]
    public void Contract_SagaBuilder_CanAddStepWithCompensation()
    {
        // Arrange & Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity")
            .Compensate("CompensateActivity")
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps.Should().HaveCount(1);
        saga.Steps[0].CompensateActivityName.Should().Be("CompensateActivity");
    }

    [Fact]
    public void Contract_SagaBuilder_CanAddMultipleSteps()
    {
        // Arrange & Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("Step1").Execute("Activity1")
            .Step("Step2").Execute("Activity2")
            .Step("Step3").Execute("Activity3")
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps.Should().HaveCount(3);
        saga.Steps[0].StepName.Should().Be("Step1");
        saga.Steps[1].StepName.Should().Be("Step2");
        saga.Steps[2].StepName.Should().Be("Step3");
    }

    [Fact]
    public void Contract_SagaBuilder_SupportsRetryConfiguration()
    {
        // Arrange
        var retryPolicy = new RetryPolicy(3, TimeSpan.FromSeconds(1));
        var taskOptions = TaskOptions.FromRetryPolicy(retryPolicy);

        // Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity")
            .WithRetry(taskOptions)
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps[0].RetryOptions.Should().NotBeNull();
    }

    [Fact]
    public void Contract_SagaBuilder_WithDefaultRetryOptions_CanBeConfigured()
    {
        // Arrange
        var defaultOptions = TaskOptions.FromRetryPolicy(new RetryPolicy(5, TimeSpan.FromSeconds(2)));

        // Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .WithDefaultRetryOptions(defaultOptions)
            .Step("TestStep")
            .Execute("TestActivity")
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps[0].RetryOptions.Should().NotBeNull();
    }

    [Fact]
    public void Contract_SagaBuilder_WithTimeout_CanBeConfigured()
    {
        // Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .WithTimeout(TimeSpan.FromMinutes(30))
            .Step("TestStep")
            .Execute("TestActivity")
            .Build();

        // Assert
        saga.Should().NotBeNull();
    }

    [Fact]
    public void Contract_SagaBuilder_SkipCompensationOnFailure_CanBeConfigured()
    {
        // Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity")
            .SkipCompensationOnFailure()
            .Build();

        // Assert
        saga.Should().NotBeNull();
        saga.Steps[0].SkipCompensationOnFailure.Should().BeTrue();
    }

    [Fact]
    public void Contract_SagaBuilder_Build_ThrowsIfNoSteps()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one step*");
    }

    [Fact]
    public void Contract_SagaBuilder_Build_ThrowsIfStepHasNoExecute()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("NoExecute");

        // Act
        var act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*must have an Execute*");
    }

    [Fact]
    public void Contract_DurableSaga_HasReadOnlySteps()
    {
        // Arrange & Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("Step1").Execute("Activity1")
            .Build();

        // Assert
        saga.Steps.Should().BeAssignableTo<IReadOnlyList<DurableSagaStep<TestSagaData>>>();
    }

    [Fact]
    public void Contract_DurableSagaStep_HasRequiredProperties()
    {
        // Arrange & Act
        var saga = DurableSagaBuilder
            .Create<TestSagaData>()
            .Step("MyStep")
            .Execute("MyActivity")
            .Compensate("MyCompensation")
            .Build();

        // Assert
        var step = saga.Steps[0];
        step.StepName.Should().Be("MyStep");
        step.ExecuteActivityName.Should().Be("MyActivity");
        step.CompensateActivityName.Should().Be("MyCompensation");
        step.SkipCompensationOnFailure.Should().BeFalse();
    }

    [Fact]
    public void Contract_DurableSagaError_HasRequiredProperties()
    {
        // Arrange
        var error = new DurableSagaError
        {
            FailedStep = "FailedStep",
            OriginalError = EncinaErrors.Create("test.error", "Test error"),
            CompensationErrors = new Dictionary<string, EncinaError?>
            {
                ["Step1"] = null,
                ["Step2"] = EncinaErrors.Create("comp.error", "Compensation failed")
            },
            WasCompensated = false
        };

        // Assert
        error.FailedStep.Should().Be("FailedStep");
        error.OriginalError.Should().NotBeNull();
        error.CompensationErrors.Should().HaveCount(2);
        error.WasCompensated.Should().BeFalse();
    }

    [Fact]
    public void Contract_StepBuilder_Step_ThrowsForNullOrEmptyName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var act1 = () => builder.Step(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => builder.Step(string.Empty);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contract_StepBuilder_Execute_ThrowsForNullOrEmptyName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep");

        // Act & Assert
        var act1 = () => builder.Execute(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => builder.Execute(string.Empty);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contract_StepBuilder_Compensate_ThrowsForNullOrEmptyName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep")
            .Execute("TestActivity");

        // Act & Assert
        var act1 = () => builder.Compensate(null!);
        act1.Should().Throw<ArgumentException>();

        var act2 = () => builder.Compensate(string.Empty);
        act2.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Contract_WithTimeout_ThrowsForZeroOrNegative()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var act1 = () => builder.WithTimeout(TimeSpan.Zero);
        act1.Should().Throw<ArgumentOutOfRangeException>();

        var act2 = () => builder.WithTimeout(TimeSpan.FromSeconds(-1));
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    private sealed record TestSagaData
    {
        public string Value { get; init; } = string.Empty;
    }
}
