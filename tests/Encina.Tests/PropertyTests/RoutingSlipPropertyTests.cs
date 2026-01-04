using Encina.Messaging.RoutingSlip;
using Encina.Testing.FsCheck;
using FsCheck;
using FsCheck.Fluent;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.PropertyTests;

/// <summary>
/// Property-based tests for Routing Slip pattern.
/// Verifies invariants and properties that should hold across various inputs.
/// </summary>
public sealed class RoutingSlipPropertyTests
{
    private readonly IRequestContext _requestContext = RequestContext.Create();
    private readonly RoutingSlipOptions _options = new();
    private readonly ILogger<RoutingSlipRunner> _logger = Substitute.For<ILogger<RoutingSlipRunner>>();

    #region Step Execution Invariants

    /// <summary>
    /// Property: Steps always execute in order.
    /// Invariant: Step N executes before Step N+1.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task StepOrder_AlwaysSequential(int stepCount)
    {
        // Arrange
        var executionOrder = new List<int>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");
        for (var i = 1; i <= stepCount; i++)
        {
            var stepIndex = i;
            builder = builder.Step($"Step {stepIndex}")
                .Execute((data, ctx, ct) =>
                {
                    executionOrder.Add(stepIndex);
                    return ValueTask.FromResult(Right<EncinaError, TestData>(data));
                })
                .Build().SlipType == "TestSlip" ? RoutingSlipBuilder.Create<TestData>("TestSlip") : builder;
        }

        // Rebuild properly with chained steps
        builder = RoutingSlipBuilder.Create<TestData>("TestSlip");
        var stepBuilder = builder.Step("Step 1").Execute((data, ctx, ct) =>
        {
            executionOrder.Add(1);
            return ValueTask.FromResult(Right<EncinaError, TestData>(data));
        });

        for (var i = 2; i <= stepCount; i++)
        {
            var stepIndex = i;
            stepBuilder = stepBuilder.Step($"Step {stepIndex}")
                .Execute((data, ctx, ct) =>
                {
                    executionOrder.Add(stepIndex);
                    return ValueTask.FromResult(Right<EncinaError, TestData>(data));
                });
        }

        var definition = stepBuilder.Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.ShouldBeSuccess();
        executionOrder.ShouldBe(Enumerable.Range(1, stepCount).ToList());
    }

    /// <summary>
    /// Property: Step count always matches definition.
    /// Invariant: Result.StepsExecuted = Definition.Steps.Count (when all succeed).
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task StepCount_MatchesDefinition(int stepCount)
    {
        // Arrange
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
        var definition = BuildDefinitionWithSteps(stepCount);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var slipResult = result.ShouldBeSuccess();
        slipResult.StepsExecuted.ShouldBe(stepCount);
    }

    /// <summary>
    /// Property: Empty data allowed (no steps).
    /// Invariant: Builder requires at least one step.
    /// </summary>
    [Fact]
    public void EmptyDefinition_ThrowsException()
    {
        // Arrange
        var builder = RoutingSlipBuilder.Create<TestData>("TestSlip");

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    #endregion

    #region Data Flow Invariants

    /// <summary>
    /// Property: Data modifications persist across steps.
    /// Invariant: Data modified in Step N is visible in Step N+1.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task DataFlow_ModificationsPersist(int stepCount)
    {
        // Arrange
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                data.Value = 1;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            });

        for (var i = 2; i <= stepCount; i++)
        {
            var expected = i;
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) =>
                {
                    // Each step should see previous step's value
                    data.Value.ShouldBe(expected - 1);
                    data.Value = expected;
                    return ValueTask.FromResult(Right<EncinaError, TestData>(data));
                });
        }

        var definition = stepBuilder.Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var slipResult = result.ShouldBeSuccess();
        slipResult.FinalData.Value.ShouldBe(stepCount);
    }

    /// <summary>
    /// Property: Activity log records all executed steps.
    /// Invariant: ActivityLog.Count = StepsExecuted.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ActivityLog_RecordsAllSteps(int stepCount)
    {
        // Arrange
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
        var definition = BuildDefinitionWithSteps(stepCount);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var slipResult = result.ShouldBeSuccess();
        slipResult.ActivityLog.Count.ShouldBe(stepCount);
    }

    #endregion

    #region Compensation Invariants

    /// <summary>
    /// Property: Compensation runs in reverse order.
    /// Invariant: Step N compensated before Step N-1.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Compensation_RunsInReverseOrder(int successfulSteps)
    {
        // Arrange
        var compensationOrder = new List<int>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ctx, ct) =>
            {
                compensationOrder.Add(1);
                return Task.CompletedTask;
            });

        for (var i = 2; i <= successfulSteps; i++)
        {
            var stepIndex = i;
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)))
                .Compensate((data, ctx, ct) =>
                {
                    compensationOrder.Add(stepIndex);
                    return Task.CompletedTask;
                });
        }

        // Add failing step
        stepBuilder = stepBuilder.Step("Failing Step")
            .Execute((data, ctx, ct) => ValueTask.FromResult(Left<EncinaError, TestData>(
                EncinaErrors.Create("fail", "Failed"))));

        var definition = stepBuilder.Build();

        // Act
        await runner.RunAsync(definition, new TestData());

        // Assert
        var expectedOrder = Enumerable.Range(1, successfulSteps).Reverse().ToList();
        compensationOrder.ShouldBe(expectedOrder);
    }

    /// <summary>
    /// Property: Only executed steps get compensated.
    /// Invariant: Failed step and subsequent steps are not compensated.
    /// </summary>
    [Theory]
    [InlineData(3, 2)] // 3 steps, fail at step 2
    [InlineData(5, 3)] // 5 steps, fail at step 3
    [InlineData(10, 1)] // 10 steps, fail at step 1
    public async Task Compensation_OnlyForExecutedSteps(int totalSteps, int failAtStep)
    {
        // Arrange
        var compensatedSteps = new System.Collections.Generic.HashSet<int>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) => failAtStep == 1
                ? ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed")))
                : ValueTask.FromResult(Right<EncinaError, TestData>(data)))
            .Compensate((data, ctx, ct) =>
            {
                compensatedSteps.Add(1);
                return Task.CompletedTask;
            });

        for (var i = 2; i <= totalSteps; i++)
        {
            var stepIndex = i;
            var shouldFail = stepIndex == failAtStep;
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) => shouldFail
                    ? ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed")))
                    : ValueTask.FromResult(Right<EncinaError, TestData>(data)))
                .Compensate((data, ctx, ct) =>
                {
                    compensatedSteps.Add(stepIndex);
                    return Task.CompletedTask;
                });
        }

        var definition = stepBuilder.Build();

        // Act
        await runner.RunAsync(definition, new TestData());

        // Assert - Only steps before the failing step should be compensated
        var expectedCompensated = new System.Collections.Generic.HashSet<int>(Enumerable.Range(1, failAtStep - 1));
        compensatedSteps.SetEquals(expectedCompensated).ShouldBeTrue();
    }

    #endregion

    #region Dynamic Route Modification Invariants

    /// <summary>
    /// Property: Dynamically added steps execute.
    /// Invariant: StepsAdded > 0 ⇒ StepsExecuted > InitialStepCount.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]  // 1 initial, add 1
    [InlineData(2, 3)]  // 2 initial, add 3
    [InlineData(5, 5)]  // 5 initial, add 5
    public async Task DynamicSteps_AreExecuted(int initialSteps, int addedSteps)
    {
        // Arrange
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Initial Step 1")
            .Execute((data, ctx, ct) =>
            {
                // Add dynamic steps
                for (var i = 0; i < addedSteps; i++)
                {
                    ctx.AddStep(new RoutingSlipStepDefinition<TestData>(
                        $"Dynamic Step {i + 1}",
                        (d, c, t) =>
                        {
                            d.Value++;
                            return ValueTask.FromResult(Right<EncinaError, TestData>(d));
                        }));
                }
                data.Value = 1;
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            });

        for (var i = 2; i <= initialSteps; i++)
        {
            stepBuilder = stepBuilder.Step($"Initial Step {i}")
                .Execute((data, ctx, ct) =>
                {
                    data.Value++;
                    return ValueTask.FromResult(Right<EncinaError, TestData>(data));
                });
        }

        var definition = stepBuilder.Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var slipResult = result.ShouldBeSuccess();
        slipResult.StepsExecuted.ShouldBe(initialSteps + addedSteps);
        slipResult.StepsAdded.ShouldBe(addedSteps);
    }

    /// <summary>
    /// Property: ClearRemainingSteps stops execution.
    /// Invariant: ClearRemainingSteps() ⇒ No more steps execute.
    /// </summary>
    [Theory]
    [InlineData(5, 2)]  // 5 steps, clear after 2
    [InlineData(10, 1)] // 10 steps, clear after 1
    [InlineData(10, 5)] // 10 steps, clear after 5
    public async Task ClearRemainingSteps_StopsExecution(int totalSteps, int clearAfterStep)
    {
        // Arrange
        var executedSteps = new List<int>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                executedSteps.Add(1);
                if (clearAfterStep == 1) ctx.ClearRemainingSteps();
                return ValueTask.FromResult(Right<EncinaError, TestData>(data));
            });

        for (var i = 2; i <= totalSteps; i++)
        {
            var stepIndex = i;
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) =>
                {
                    executedSteps.Add(stepIndex);
                    if (stepIndex == clearAfterStep) ctx.ClearRemainingSteps();
                    return ValueTask.FromResult(Right<EncinaError, TestData>(data));
                });
        }

        var definition = stepBuilder.Build();

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        result.ShouldBeSuccess();
        executedSteps.Count.ShouldBe(clearAfterStep);
        executedSteps.ShouldBe(Enumerable.Range(1, clearAfterStep).ToList());
    }

    #endregion

    #region Error Handling Invariants

    /// <summary>
    /// Property: First error stops execution.
    /// Invariant: Step failure ⇒ No subsequent steps execute.
    /// </summary>
    [Theory]
    [InlineData(5, 1)]  // Fail at first step
    [InlineData(5, 3)]  // Fail at middle step
    [InlineData(10, 5)] // Fail at step 5 of 10
    public async Task FirstError_StopsExecution(int totalSteps, int failAtStep)
    {
        // Arrange
        var executedSteps = new List<int>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);

        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) =>
            {
                executedSteps.Add(1);
                return failAtStep == 1
                    ? ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed")))
                    : ValueTask.FromResult(Right<EncinaError, TestData>(data));
            });

        for (var i = 2; i <= totalSteps; i++)
        {
            var stepIndex = i;
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) =>
                {
                    executedSteps.Add(stepIndex);
                    return stepIndex == failAtStep
                        ? ValueTask.FromResult(Left<EncinaError, TestData>(EncinaErrors.Create("fail", "Failed")))
                        : ValueTask.FromResult(Right<EncinaError, TestData>(data));
                });
        }

        var definition = stepBuilder.Build();

        // Act
        await runner.RunAsync(definition, new TestData());

        // Assert
        executedSteps.Count.ShouldBe(failAtStep);
    }

    #endregion

    #region RoutingSlipId Invariants

    /// <summary>
    /// Property: RoutingSlipId is always unique.
    /// Invariant: Multiple runs produce different IDs.
    /// </summary>
    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task RoutingSlipId_AlwaysUnique(int runCount)
    {
        // Arrange
        var ids = new System.Collections.Generic.HashSet<Guid>();
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
        var definition = BuildDefinitionWithSteps(1);

        // Act
        for (var i = 0; i < runCount; i++)
        {
            var result = await runner.RunAsync(definition, new TestData());
            _ = result.Match(
                Right: r => ids.Add(r.RoutingSlipId),
                Left: _ => false);
        }

        // Assert
        ids.Count.ShouldBe(runCount);
    }

    #endregion

    #region Duration Invariants

    /// <summary>
    /// Property: Duration is always positive.
    /// Invariant: Duration > TimeSpan.Zero.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Duration_AlwaysPositive(int stepCount)
    {
        // Arrange
        var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
        var definition = BuildDefinitionWithSteps(stepCount);

        // Act
        var result = await runner.RunAsync(definition, new TestData());

        // Assert
        var slipResult = result.ShouldBeSuccess();
        slipResult.Duration.ShouldBeGreaterThan(TimeSpan.Zero);
    }

    #endregion

    #region Test Helpers

    private static BuiltRoutingSlipDefinition<TestData> BuildDefinitionWithSteps(int stepCount)
    {
        var stepBuilder = RoutingSlipBuilder.Create<TestData>("TestSlip")
            .Step("Step 1")
            .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));

        for (var i = 2; i <= stepCount; i++)
        {
            stepBuilder = stepBuilder.Step($"Step {i}")
                .Execute((data, ctx, ct) => ValueTask.FromResult(Right<EncinaError, TestData>(data)));
        }

        return stepBuilder.Build();
    }

    private sealed class TestData
    {
        public int Value { get; set; }
    }

    #endregion

    #region FsCheck Property Tests

    /// <summary>
    /// Property: StepsExecuted always equals step count when all succeed.
    /// Verified across random step counts.
    /// </summary>
    [EncinaProperty]
    public Property StepsExecuted_EqualsStepCount_WhenAllSucceed()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 15)),
            async stepCount =>
            {
                var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
                var definition = BuildDefinitionWithSteps(stepCount);

                var result = await runner.RunAsync(definition, new TestData());

                return result.Match(
                    Left: _ => false,
                    Right: r => r.StepsExecuted == stepCount);
            });
    }

    /// <summary>
    /// Property: ActivityLog count equals StepsExecuted.
    /// Verified across random step counts.
    /// </summary>
    [EncinaProperty]
    public Property ActivityLog_CountEqualsStepsExecuted()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 15)),
            async stepCount =>
            {
                var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
                var definition = BuildDefinitionWithSteps(stepCount);

                var result = await runner.RunAsync(definition, new TestData());

                return result.Match(
                    Left: _ => false,
                    Right: r => r.ActivityLog.Count == r.StepsExecuted);
            });
    }

    /// <summary>
    /// Property: Duration is always positive for any step count.
    /// Verified with random step counts.
    /// </summary>
    [EncinaProperty]
    public Property Duration_AlwaysPositive_ForAnyStepCount()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10)),
            async stepCount =>
            {
                var runner = new RoutingSlipRunner(_requestContext, _options, _logger);
                var definition = BuildDefinitionWithSteps(stepCount);

                var result = await runner.RunAsync(definition, new TestData());

                return result.Match(
                    Left: _ => false,
                    Right: r => r.Duration > TimeSpan.Zero);
            });
    }

    #endregion
}
