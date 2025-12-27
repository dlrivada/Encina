using Encina.AzureFunctions.Durable;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.DurableTask;

namespace Encina.AzureFunctions.PropertyTests.Durable;

/// <summary>
/// Property-based tests for DurableSagaBuilder to ensure builder invariants.
/// </summary>
public sealed class DurableSagaBuilderProperties
{
    [Property(MaxTest = 50)]
    public bool AddingSteps_IncreasesStepCount(PositiveInt stepCount)
    {
        // Arrange
        var count = Math.Min(stepCount.Get, 10); // Limit to 10 steps for performance
        if (count == 0) return true;

        // Act - Build chain of steps
        DurableSagaStepBuilder<TestData>? stepBuilder = null;
        var builder = DurableSagaBuilder.Create<TestData>();

        for (var i = 0; i < count; i++)
        {
            if (stepBuilder == null)
            {
                stepBuilder = builder.Step($"Step{i}").Execute($"Activity{i}");
            }
            else
            {
                stepBuilder = stepBuilder.Step($"Step{i}").Execute($"Activity{i}");
            }
        }

        var saga = stepBuilder!.Build();

        // Assert
        return saga.Steps.Count == count;
    }

    [Property(MaxTest = 50)]
    public bool StepsPreserveOrder(PositiveInt stepCount)
    {
        // Arrange
        var count = Math.Min(stepCount.Get, 5); // Limit to 5 steps for performance
        if (count == 0) return true;

        var builder = DurableSagaBuilder.Create<TestData>();
        DurableSagaStepBuilder<TestData>? stepBuilder = null;

        // Act - Add steps with indexed names
        for (var i = 0; i < count; i++)
        {
            if (stepBuilder == null)
            {
                stepBuilder = builder.Step($"Step{i}").Execute($"Activity{i}");
            }
            else
            {
                stepBuilder = stepBuilder.Step($"Step{i}").Execute($"Activity{i}");
            }
        }

        var result = stepBuilder!.Build();

        // Assert - Steps should be in order
        for (var i = 0; i < count; i++)
        {
            if (result.Steps[i].StepName != $"Step{i}")
            {
                return false;
            }
        }

        return true;
    }

    [Property(MaxTest = 50)]
    public bool ActivityNamesArePreserved(NonEmptyString stepName, NonEmptyString activityName)
    {
        // Skip names that would cause issues
        if (stepName.Get.Contains('\0') || activityName.Get.Contains('\0'))
        {
            return true;
        }

        // Arrange & Act
        var saga = DurableSagaBuilder.Create<TestData>()
            .Step(stepName.Get)
            .Execute(activityName.Get)
            .Build();

        // Assert
        return saga.Steps[0].StepName == stepName.Get &&
               saga.Steps[0].ExecuteActivityName == activityName.Get;
    }

    [Property(MaxTest = 50)]
    public bool CompensationNamesArePreserved(NonEmptyString stepName, NonEmptyString activityName, NonEmptyString compensateName)
    {
        // Skip names that would cause issues
        if (stepName.Get.Contains('\0') || activityName.Get.Contains('\0') || compensateName.Get.Contains('\0'))
        {
            return true;
        }

        // Arrange & Act
        var saga = DurableSagaBuilder.Create<TestData>()
            .Step(stepName.Get)
            .Execute(activityName.Get)
            .Compensate(compensateName.Get)
            .Build();

        // Assert
        return saga.Steps[0].CompensateActivityName == compensateName.Get;
    }

    [Property(MaxTest = 30)]
    public bool RetryOptionsArePreserved(PositiveInt maxRetries)
    {
        // Arrange
        var retries = Math.Min(maxRetries.Get, 100); // Reasonable limit
        var retryPolicy = new RetryPolicy(retries, TimeSpan.FromSeconds(1));
        var taskOptions = TaskOptions.FromRetryPolicy(retryPolicy);

        // Act
        var saga = DurableSagaBuilder.Create<TestData>()
            .Step("TestStep")
            .Execute("TestActivity")
            .WithRetry(taskOptions)
            .Build();

        // Assert
        return saga.Steps[0].RetryOptions != null;
    }

    [Property(MaxTest = 30)]
    public bool DefaultRetryOptionsAreAppliedToAllSteps(PositiveInt maxRetries, PositiveInt stepCount)
    {
        // Arrange
        var retries = Math.Min(maxRetries.Get, 100);
        var count = Math.Min(stepCount.Get, 5);
        if (count == 0) return true;

        var retryPolicy = new RetryPolicy(retries, TimeSpan.FromSeconds(1));
        var taskOptions = TaskOptions.FromRetryPolicy(retryPolicy);

        var builder = DurableSagaBuilder.Create<TestData>()
            .WithDefaultRetryOptions(taskOptions);

        // Act
        DurableSagaStepBuilder<TestData>? stepBuilder = null;
        for (var i = 0; i < count; i++)
        {
            if (stepBuilder == null)
            {
                stepBuilder = builder.Step($"Step{i}").Execute($"Activity{i}");
            }
            else
            {
                stepBuilder = stepBuilder.Step($"Step{i}").Execute($"Activity{i}");
            }
        }

        var saga = stepBuilder!.Build();

        // Assert - All steps should have retry options
        return saga.Steps.All(s => s.RetryOptions != null);
    }

    [Property(MaxTest = 30)]
    public bool SkipCompensationOnFailureIsPreserved(bool skipCompensation)
    {
        // Arrange & Act
        var stepBuilder = DurableSagaBuilder.Create<TestData>()
            .Step("TestStep")
            .Execute("TestActivity");

        if (skipCompensation)
        {
            stepBuilder = stepBuilder.SkipCompensationOnFailure();
        }

        var saga = stepBuilder.Build();

        // Assert
        return saga.Steps[0].SkipCompensationOnFailure == skipCompensation;
    }

    private sealed record TestData
    {
        public string Value { get; init; } = string.Empty;
    }
}
