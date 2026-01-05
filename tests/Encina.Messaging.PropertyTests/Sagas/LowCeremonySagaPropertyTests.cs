using Encina.Messaging.Sagas.LowCeremony;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Shouldly;
using Xunit;
using static LanguageExt.Prelude;

namespace Encina.Messaging.PropertyTests.Sagas;

/// <summary>
/// Property-based tests for low-ceremony saga invariants.
/// </summary>
public sealed class LowCeremonySagaPropertyTests
{
    /// <summary>
    /// Generator for non-whitespace strings.
    /// FsCheck's NonEmptyString can generate whitespace-only strings like "\n"
    /// which pass non-empty check but fail ThrowIfNullOrWhiteSpace validation.
    /// This generator ensures we only get strings with at least one non-whitespace character.
    /// </summary>
    private static Arbitrary<string> NonWhitespaceStringArb() =>
        ArbMap.Default.ArbFor<NonEmptyString>()
            .Filter(s => !string.IsNullOrWhiteSpace(s.Get))
            .Convert(s => s.Get, NonEmptyString.NewNonEmptyString);

    /// <summary>
    /// Generator for arrays of non-whitespace strings.
    /// Uses NonEmptyListOf to guarantee non-empty arrays even during shrinking.
    /// </summary>
    private static Arbitrary<string[]> NonWhitespaceStringArrayArb() =>
        NonWhitespaceStringArb().Generator
            .NonEmptyListOf(10)
            .Select(list => list.ToArray())
            .ToArbitrary();

    /// <summary>
    /// SagaDefinition should preserve saga type name.
    /// </summary>
    [Property(MaxTest = 100)]
    public Property SagaDefinition_PreservesSagaType() =>
        FsCheck.Fluent.Prop.ForAll(NonWhitespaceStringArb(), sagaType =>
        {
            var definition = SagaDefinition.Create<TestData>(sagaType);
            return definition.SagaType == sagaType;
        });

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
    public Property BuiltSagaDefinition_PreservesStepNamesInOrder() =>
        FsCheck.Fluent.Prop.ForAll(NonWhitespaceStringArrayArb(), stepNames =>
        {
            var names = stepNames.Take(10).ToArray();
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

            return builtSaga.Steps.Select(s => s.Name).SequenceEqual(names);
        });

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
    /// Uses fixed values because the invariant (timeout defaults to null) is independent
    /// of saga type or step names - it's a structural property of the builder.
    /// </summary>
    [Fact]
    public void SagaDefinition_NoTimeout_IsNull()
    {
        // Arrange
        var builder = SagaDefinition.Create<TestData>(nameof(TestData))
            .Step("Test")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act
        var builtSaga = builder.Build();

        // Assert
        builtSaga.Timeout.ShouldBeNull();
    }

    /// <summary>
    /// Steps with compensation should have non-null Compensate.
    /// Uses fixed values because the invariant (Compensate is non-null when defined)
    /// is independent of saga type or step names - it's a structural builder guarantee.
    /// </summary>
    [Fact]
    public void SagaStep_WithCompensation_HasNonNullCompensate()
    {
        // Arrange
        var builder = SagaDefinition.Create<TestData>("TestSaga")
            .Step("TestStep")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ct) => Task.CompletedTask);

        // Act
        var builtSaga = builder.Build();

        // Assert
        builtSaga.Steps[0].Compensate.ShouldNotBeNull();
    }

    /// <summary>
    /// Steps without compensation should have null Compensate.
    /// Uses fixed values because the invariant (Compensate is null when not defined)
    /// is independent of saga type or step names - it's a structural builder guarantee.
    /// </summary>
    [Fact]
    public void SagaStep_WithoutCompensation_HasNullCompensate()
    {
        // Arrange
        var builder = SagaDefinition.Create<TestData>("TestSaga")
            .Step("TestStep")
            .Execute((data, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        // Act
        var builtSaga = builder.Build();

        // Assert
        builtSaga.Steps[0].Compensate.ShouldBeNull();
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
