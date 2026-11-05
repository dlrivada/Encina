using Encina.AzureFunctions.Durable;
using LanguageExt;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Runs <see cref="DurableSagaBuilder{TData}"/> sagas on the Durable Task in-process test host: the real
/// orchestration engine replays the orchestrator and runs the activities. Error codes are asserted, never
/// messages.
/// </summary>
[Collection("Durable-TestHost")]
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class DurableSagaOnTestHostIntegrationTests
{
    private const string Orchestrator = "SagaTh_Orchestrator";
    private const string FailureCode = "saga.test_failure";
    private static readonly string[] Steps = ["A", "B", "C"];

    private static ActivityResult<DurableSagaTestData> Append(DurableSagaTestData input, string mark)
        => ActivityResult<DurableSagaTestData>.Success(new DurableSagaTestData { Trail = input.Trail + mark });

    private static DurableSagaOutcome ToOutcome(Either<DurableSagaError, DurableSagaTestData> result)
        => result.Match(
            Right: data => new DurableSagaOutcome { Succeeded = true, Trail = data.Trail },
            Left: error => new DurableSagaOutcome
            {
                FailedStep = error.FailedStep,
                ErrorCode = error.OriginalError.GetCode().IfNone(string.Empty),
                WasCompensated = error.WasCompensated,
                CompensationFailures = [.. error.CompensationErrors.Where(c => c.Value is not null).Select(c => c.Key)],
                CompensatedSteps = [.. error.CompensationErrors.Where(c => c.Value is null).Select(c => c.Key)]
            });

    private static void RegisterSaga(DurableTaskRegistry registry, DurableSaga<DurableSagaTestData> saga)
    {
        registry.AddOrchestratorFunc<DurableSagaTestData, DurableSagaOutcome>(
            "SagaTh_Orchestrator", // a literal: the Durable analyzers crash on a constant or variable name
            async (context, input) => ToOutcome(await saga.ExecuteAsync(context, input)));
    }

    private static void RegisterActivity(
        DurableTaskRegistry registry,
        string name,
        List<string> calls,
        Func<DurableSagaTestData, ActivityResult<DurableSagaTestData>> body)
    {
        registry.AddActivityFunc<DurableSagaTestData, ActivityResult<DurableSagaTestData>>(
            name,
            (_, input) =>
            {
                lock (calls)
                {
                    calls.Add(name);
                }

                return Task.FromResult(body(input));
            });
    }

    private static DurableSaga<DurableSagaTestData> BuildSaga(
        string prefix,
        string[] steps,
        bool compensate = true)
    {
        var builder = DurableSagaBuilder.Create<DurableSagaTestData>();
        DurableSagaStepBuilder<DurableSagaTestData>? last = null;
        foreach (var step in steps)
        {
            var stepBuilder = (last?.Step(step) ?? builder.Step(step)).Execute($"{prefix}_{step}");
            last = compensate ? stepBuilder.Compensate($"{prefix}_Undo{step}") : stepBuilder;
        }

        return last!.Build().Match(Right: saga => saga, Left: _ => throw new InvalidOperationException("saga did not build"));
    }

    [Fact]
    public async Task ExecuteAsync_AllStepsSucceed_ReturnsTheFinalDataInStepOrder()
    {
        // Arrange
        var calls = new List<string>();
        var saga = BuildSaga("SagaThOk", ["A", "B", "C"]);
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
        {
            RegisterSaga(registry, saga);
            foreach (var step in Steps)
            {
                RegisterActivity(registry, $"SagaThOk_{step}", calls, d => Append(d, step));
                RegisterActivity(registry, $"SagaThOk_Undo{step}", calls, d => Append(d, "x"));
            }
        });

        // Act
        var metadata = await DurableTestHostSupport.RunAsync(host, Orchestrator, new DurableSagaTestData());

        // Assert
        metadata.RuntimeStatus.ShouldBe(OrchestrationRuntimeStatus.Completed);
        var outcome = metadata.ReadOutputAs<DurableSagaOutcome>()!;
        outcome.Succeeded.ShouldBeTrue();
        outcome.Trail.ShouldBe("ABC");
        calls.ShouldBe(["SagaThOk_A", "SagaThOk_B", "SagaThOk_C"]);
    }

    [Fact]
    public async Task ExecuteAsync_LaterStepFails_CompensatesExecutedStepsInReverseAndReturnsTheStepError()
    {
        // Arrange
        var calls = new List<string>();
        var saga = BuildSaga("SagaThFail", ["A", "B", "C"]);
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
        {
            RegisterSaga(registry, saga);
            RegisterActivity(registry, "SagaThFail_A", calls, d => Append(d, "A"));
            RegisterActivity(registry, "SagaThFail_B", calls, d => Append(d, "B"));
            RegisterActivity(registry, "SagaThFail_C", calls,
                _ => ActivityResult<DurableSagaTestData>.Failure(FailureCode, "step C refused"));
            foreach (var step in Steps)
            {
                RegisterActivity(registry, $"SagaThFail_Undo{step}", calls, d => Append(d, "x"));
            }
        });

        // Act
        var metadata = await DurableTestHostSupport.RunAsync(host, Orchestrator, new DurableSagaTestData());

        // Assert
        metadata.RuntimeStatus.ShouldBe(OrchestrationRuntimeStatus.Completed);
        var outcome = metadata.ReadOutputAs<DurableSagaOutcome>()!;
        outcome.Succeeded.ShouldBeFalse();
        outcome.FailedStep.ShouldBe("C");
        outcome.ErrorCode.ShouldBe(FailureCode);
        outcome.WasCompensated.ShouldBeTrue();
        outcome.CompensatedSteps.ShouldBe(["B", "A"]);
        calls.ShouldBe(["SagaThFail_A", "SagaThFail_B", "SagaThFail_C", "SagaThFail_UndoB", "SagaThFail_UndoA"]);
    }

    [Fact]
    public async Task ExecuteAsync_CompensationReturnsAnError_RecordsItAndReportsNotCompensated()
    {
        // Arrange
        var calls = new List<string>();
        var saga = BuildSaga("SagaThComp", ["A", "B"]);
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
        {
            RegisterSaga(registry, saga);
            RegisterActivity(registry, "SagaThComp_A", calls, d => Append(d, "A"));
            RegisterActivity(registry, "SagaThComp_B", calls,
                _ => ActivityResult<DurableSagaTestData>.Failure(FailureCode, "step B refused"));
            RegisterActivity(registry, "SagaThComp_UndoA", calls,
                _ => ActivityResult<DurableSagaTestData>.Failure("saga.undo_failure", "undo A refused"));
            RegisterActivity(registry, "SagaThComp_UndoB", calls, d => Append(d, "x"));
        });

        // Act
        var metadata = await DurableTestHostSupport.RunAsync(host, Orchestrator, new DurableSagaTestData());

        // Assert
        var outcome = metadata.ReadOutputAs<DurableSagaOutcome>()!;
        outcome.FailedStep.ShouldBe("B");
        outcome.ErrorCode.ShouldBe(FailureCode);
        outcome.WasCompensated.ShouldBeFalse();
        outcome.CompensationFailures.ShouldBe(["A"]);
    }

    [Fact]
    public async Task ExecuteAsync_ActivityThrows_ReturnsTheActivityFailedErrorAndCompensates()
    {
        // Arrange
        var saga = BuildSaga("SagaThThrow", ["A", "B"]);
        var calls = new List<string>();
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
        {
            RegisterSaga(registry, saga);
            RegisterActivity(registry, "SagaThThrow_A", calls, d => Append(d, "A"));
            RegisterActivity(registry, "SagaThThrow_B", calls, _ => throw new InvalidOperationException("boom"));
            RegisterActivity(registry, "SagaThThrow_UndoA", calls, d => Append(d, "x"));
            RegisterActivity(registry, "SagaThThrow_UndoB", calls, d => Append(d, "x"));
        });

        // Act
        var metadata = await DurableTestHostSupport.RunAsync(host, Orchestrator, new DurableSagaTestData());

        // Assert
        var outcome = metadata.ReadOutputAs<DurableSagaOutcome>()!;
        outcome.FailedStep.ShouldBe("B");
        outcome.ErrorCode.ShouldBe("durable.activity_failed");
        outcome.CompensatedSteps.ShouldBe(["A"]);
    }

    [Fact]
    public async Task ExecuteAsync_StepWithoutCompensation_IsNotCompensated()
    {
        // Arrange
        var calls = new List<string>();
        var saga = BuildSaga("SagaThNoComp", ["A", "B"], compensate: false);
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
        {
            RegisterSaga(registry, saga);
            RegisterActivity(registry, "SagaThNoComp_A", calls, d => Append(d, "A"));
            RegisterActivity(registry, "SagaThNoComp_B", calls,
                _ => ActivityResult<DurableSagaTestData>.Failure(FailureCode, "step B refused"));
        });

        // Act
        var metadata = await DurableTestHostSupport.RunAsync(host, Orchestrator, new DurableSagaTestData());

        // Assert
        var outcome = metadata.ReadOutputAs<DurableSagaOutcome>()!;
        outcome.FailedStep.ShouldBe("B");
        outcome.CompensatedSteps.ShouldBeEmpty();
        outcome.WasCompensated.ShouldBeTrue();
        calls.ShouldBe(["SagaThNoComp_A", "SagaThNoComp_B"]);
    }
}
