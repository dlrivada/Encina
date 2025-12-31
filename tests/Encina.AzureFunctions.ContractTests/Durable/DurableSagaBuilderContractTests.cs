using Encina.AzureFunctions.Durable;
using Shouldly;
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
        builder.ShouldNotBeNull();
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
        saga.ShouldNotBeNull();
        saga.Steps.Count.ShouldBe(1);
        saga.Steps[0].StepName.ShouldBe("TestStep");
        saga.Steps[0].ExecuteActivityName.ShouldBe("TestActivity");
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
        saga.ShouldNotBeNull();
        saga.Steps.Count.ShouldBe(1);
        saga.Steps[0].CompensateActivityName.ShouldBe("CompensateActivity");
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
        saga.ShouldNotBeNull();
        saga.Steps.Count.ShouldBe(3);
        saga.Steps[0].StepName.ShouldBe("Step1");
        saga.Steps[1].StepName.ShouldBe("Step2");
        saga.Steps[2].StepName.ShouldBe("Step3");
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
        saga.ShouldNotBeNull();
        saga.Steps[0].RetryOptions.ShouldNotBeNull();
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
        saga.ShouldNotBeNull();
        saga.Steps[0].RetryOptions.ShouldNotBeNull();
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
        saga.ShouldNotBeNull();
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
        saga.ShouldNotBeNull();
        saga.Steps[0].SkipCompensationOnFailure.ShouldBeTrue();
    }

    [Fact]
    public void Contract_SagaBuilder_Build_ThrowsIfNoSteps()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act
        var act = () => builder.Build();

        // Assert
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*at least one step*");
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
        var ex = Should.Throw<InvalidOperationException>(act);
        ex.Message.ShouldMatch("*must have an Execute*");
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
        saga.Steps.ShouldBeAssignableTo<IReadOnlyList<DurableSagaStep<TestSagaData>>>();
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
        step.StepName.ShouldBe("MyStep");
        step.ExecuteActivityName.ShouldBe("MyActivity");
        step.CompensateActivityName.ShouldBe("MyCompensation");
        step.SkipCompensationOnFailure.ShouldBeFalse();
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
        error.FailedStep.ShouldBe("FailedStep");
        error.OriginalError.ShouldNotBeNull();
        error.CompensationErrors.Count.ShouldBe(2);
        error.WasCompensated.ShouldBeFalse();
    }

    [Fact]
    public void Contract_StepBuilder_Step_ThrowsForNullOrEmptyName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var act1 = () => builder.Step(null!);
        var ex1 = Should.Throw<ArgumentException>(act1);
        ex1.ParamName.ShouldBe("stepName");

        var act2 = () => builder.Step(string.Empty);
        var ex2 = Should.Throw<ArgumentException>(act2);
        ex2.ParamName.ShouldBe("stepName");
    }

    [Fact]
    public void Contract_StepBuilder_Execute_ThrowsForNullOrEmptyName()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>()
            .Step("TestStep");

        // Act & Assert
        var act1 = () => builder.Execute(null!);
        var ex1 = Should.Throw<ArgumentException>(act1);
        ex1.ParamName.ShouldBe("activityName");

        var act2 = () => builder.Execute(string.Empty);
        var ex2 = Should.Throw<ArgumentException>(act2);
        ex2.ParamName.ShouldBe("activityName");
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
        var ex1 = Should.Throw<ArgumentException>(act1);
        ex1.ParamName.ShouldBe("activityName");

        var act2 = () => builder.Compensate(string.Empty);
        var ex2 = Should.Throw<ArgumentException>(act2);
        ex2.ParamName.ShouldBe("activityName");
    }

    [Fact]
    public void Contract_WithTimeout_ThrowsForZeroOrNegative()
    {
        // Arrange
        var builder = DurableSagaBuilder.Create<TestSagaData>();

        // Act & Assert
        var act1 = () => builder.WithTimeout(TimeSpan.Zero);
        var ex1 = Should.Throw<ArgumentOutOfRangeException>(act1);
        ex1.ParamName.ShouldBe("timeout");

        var act2 = () => builder.WithTimeout(TimeSpan.FromSeconds(-1));
        var ex2 = Should.Throw<ArgumentOutOfRangeException>(act2);
        ex2.ParamName.ShouldBe("timeout");
    }

    private sealed record TestSagaData
    {
        public string Value { get; init; } = string.Empty;
    }
}
