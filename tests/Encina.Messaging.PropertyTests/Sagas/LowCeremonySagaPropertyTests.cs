using Encina.Messaging.Sagas.LowCeremony;
using FsCheck;
using FsCheck.Xunit;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.Messaging.PropertyTests.Sagas;

/// <summary>
/// Property-based tests for low-ceremony saga invariants.
/// </summary>
public sealed class LowCeremonySagaPropertyTests
{
    /// <summary>
    /// SagaDefinition should preserve saga type name.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaDefinition_PreservesSagaType(NonEmptyString sagaType)
    {
        var definition = SagaDefinition.Create<TestData>(sagaType.Get);
        return definition.SagaType == sagaType.Get;
    }

    /// <summary>
    /// BuiltSagaDefinition step count should match number of added steps.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BuiltSagaDefinition_StepCountMatchesAddedSteps(PositiveInt stepCount)
    {
        // Limit to reasonable number of steps
        var count = Math.Min(stepCount.Get, 20);

        var definition = SagaDefinition.Create<TestData>("TestSaga");

        // Add first step
        var builder = definition.Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Add remaining steps
        for (var i = 2; i <= count; i++)
        {
            builder = builder.Step($"Step {i}")
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
        }

        var builtSaga = builder.Build();
        return builtSaga.StepCount == count;
    }

    /// <summary>
    /// Step names should be preserved in order.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool BuiltSagaDefinition_PreservesStepNamesInOrder(NonEmptyArray<NonEmptyString> stepNames)
    {
        var names = stepNames.Get.Select(s => s.Get).Take(10).ToArray();
        if (names.Length == 0) return true;

        var definition = SagaDefinition.Create<TestData>("TestSaga");
        var builder = definition.Step(names[0])
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        for (var i = 1; i < names.Length; i++)
        {
            builder = builder.Step(names[i])
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
        }

        var builtSaga = builder.Build();

        for (var i = 0; i < names.Length; i++)
        {
            if (builtSaga.Steps[i].Name != names[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Timeout should be preserved when set.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaDefinition_PreservesTimeout(PositiveInt minutes)
    {
        var timeout = TimeSpan.FromMinutes(Math.Min(minutes.Get, 1440)); // Max 24 hours

        var builtSaga = SagaDefinition.Create<TestData>("TestSaga")
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .WithTimeout(timeout)
            .Build();

        return builtSaga.Timeout == timeout;
    }

    /// <summary>
    /// SagaDefinition without timeout should have null timeout.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SagaDefinition_NoTimeout_IsNull(NonEmptyString sagaType)
    {
        var builtSaga = SagaDefinition.Create<TestData>(sagaType.Get)
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        return builtSaga.Timeout is null;
    }

    /// <summary>
    /// Steps with compensation should have non-null Compensate.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SagaStep_WithCompensation_HasNonNullCompensate(NonEmptyString stepName)
    {
        var builtSaga = SagaDefinition.Create<TestData>("TestSaga")
            .Step(stepName.Get)
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => Task.CompletedTask)
            .Build();

        return builtSaga.Steps[0].Compensate is not null;
    }

    /// <summary>
    /// Steps without compensation should have null Compensate.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SagaStep_WithoutCompensation_HasNullCompensate(NonEmptyString stepName)
    {
        var builtSaga = SagaDefinition.Create<TestData>("TestSaga")
            .Step(stepName.Get)
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Build();

        return builtSaga.Steps[0].Compensate is null;
    }

    /// <summary>
    /// Execute function should be non-null for all steps.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SagaStep_Execute_IsNeverNull(PositiveInt stepCount)
    {
        var count = Math.Min(stepCount.Get, 10);

        var definition = SagaDefinition.Create<TestData>("TestSaga");
        var builder = definition.Step("Step 1")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        for (var i = 2; i <= count; i++)
        {
            builder = builder.Step($"Step {i}")
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
        }

        var builtSaga = builder.Build();

        return builtSaga.Steps.All(step => step.Execute is not null);
    }

    /// <summary>
    /// SagaResult should have correct steps executed count.
    /// </summary>
    [Property(MaxTest = 50)]
    public bool SagaResult_StepsExecuted_IsCorrect(PositiveInt stepsExecuted)
    {
        var count = Math.Min(stepsExecuted.Get, 100);
        var data = new TestData { Value = 42 };
        var sagaId = Guid.NewGuid();

        var result = new SagaResult<TestData>(sagaId, data, count);

        return result.StepsExecuted == count
               && result.SagaId == sagaId
               && result.Data.Value == 42;
    }

    /// <summary>
    /// SagaResult should preserve data reference.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool SagaResult_PreservesData(int value)
    {
        var data = new TestData { Value = value };
        var result = new SagaResult<TestData>(Guid.NewGuid(), data, 1);

        return result.Data.Value == value;
    }

    /// <summary>
    /// Default step name should follow pattern "Step N".
    /// </summary>
    [Property(MaxTest = 20)]
    public bool SagaStep_DefaultName_FollowsPattern(PositiveInt stepCount)
    {
        var count = Math.Min(stepCount.Get, 10);

        var definition = SagaDefinition.Create<TestData>("TestSaga");
        var builder = definition.Step() // No name
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        for (var i = 2; i <= count; i++)
        {
            builder = builder.Step() // No name
                .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
        }

        var builtSaga = builder.Build();

        for (var i = 0; i < count; i++)
        {
            if (builtSaga.Steps[i].Name != $"Step {i + 1}")
                return false;
        }

        return true;
    }

    private sealed class TestData
    {
        public int Value { get; set; }
    }
}
