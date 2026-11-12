using Encina.AzureFunctions.Durable;
using LanguageExt;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Testing;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Runs the <see cref="FanOutFanInExtensions"/> helpers inside orchestrations on the Durable Task in-process
/// test host. One behaviour per orchestrator; error codes and details keys are asserted, never messages.
/// Orchestrator names are literals at each registration: the Durable analyzers crash on a constant or a
/// variable name, and names must be unique across the assembly.
/// </summary>
[Collection("Durable-TestHost")]
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class FanOutFanInOnTestHostIntegrationTests
{
    private static readonly List<int> ThreeInputs = [0, 1, 2];

    private static FanOutOutcome ToOutcome(Either<EncinaError, IReadOnlyList<string>> result)
        => result.Match(
            Right: values => new FanOutOutcome { IsRight = true, Values = [.. values] },
            Left: ToFailure);

    private static FanOutOutcome ToOutcome(Either<EncinaError, string> result)
        => result.Match(
            Right: value => new FanOutOutcome { IsRight = true, Values = [value] },
            Left: ToFailure);

    private static FanOutOutcome ToFailure(EncinaError error)
    {
        var details = error.GetDetails();
        var outcome = new FanOutOutcome { ErrorCode = error.GetCode().IfNone(string.Empty) };
        if (details.TryGetValue("total", out var total))
        {
            outcome.Total = (int)total!;
            outcome.FailedCount = (int)details["failedCount"]!;
            outcome.Failures =
            [
                .. ((IReadOnlyList<IReadOnlyDictionary<string, object?>>)details["failures"]!)
                    .Select(f => $"{f["index"]}:{f["code"]}")
            ];
        }

        return outcome;
    }

    private static FanOutOutcome ToOutcome(IReadOnlyList<Either<EncinaError, string>> results)
        => new()
        {
            Values = [.. results.Select(r => r.Match(Right: v => v, Left: e => e.GetCode().IfNone(string.Empty)))]
        };

    private static async Task<FanOutOutcome> RunAsync(Action<DurableTaskRegistry> register, string orchestratorName)
    {
        await using var host = await DurableTestHostSupport.StartAsync(register);
        var metadata = await DurableTestHostSupport.RunAsync(host, orchestratorName, ThreeInputs);

        metadata.RuntimeStatus.ShouldBe(OrchestrationRuntimeStatus.Completed);
        return metadata.ReadOutputAs<FanOutOutcome>()!;
    }

    [Fact]
    public async Task FanOutAsync_MixedResults_ReturnsOneEitherPerInputInOrder()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThAll_Orch",
                async (c, i) => ToOutcome(await c.FanOutAsync<int, string>("FanThAll_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThAll_Act", (_, i) =>
                Task.FromResult(i == 1 ? ActivityResult<string>.Failure("fan.refused", "refused") : ActivityResult<string>.Success($"v{i}")));
        }, "FanThAll_Orch");

        outcome.Values.ShouldBe(["v0", "fan.refused", "v2"]);
    }

    [Fact]
    public async Task FanOutAllAsync_AllSucceed_ReturnsEveryValueInInputOrder()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThAllOk_Orch",
                async (c, i) => ToOutcome(await c.FanOutAllAsync<int, string>("FanThAllOk_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThAllOk_Act", (_, i) =>
                Task.FromResult(ActivityResult<string>.Success($"v{i}")));
        }, "FanThAllOk_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Values.ShouldBe(["v0", "v1", "v2"]);
    }

    [Fact]
    public async Task FanOutAllAsync_OneFailure_ReturnsTheAggregateErrorWithTheFailureCodeAndIndex()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThAllOne_Orch",
                async (c, i) => ToOutcome(await c.FanOutAllAsync<int, string>("FanThAllOne_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThAllOne_Act", (_, i) =>
                Task.FromResult(i == 1 ? ActivityResult<string>.Failure("fan.refused", "refused") : ActivityResult<string>.Success($"v{i}")));
        }, "FanThAllOne_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.fanout_failed");
        outcome.Total.ShouldBe(3);
        outcome.FailedCount.ShouldBe(1);
        outcome.Failures.ShouldBe(["1:fan.refused"]);
    }

    [Fact]
    public async Task FanOutAllAsync_SeveralFailures_ListsEveryFailureCodeAndIndex()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThAllMany_Orch",
                async (c, i) => ToOutcome(await c.FanOutAllAsync<int, string>("FanThAllMany_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThAllMany_Act", (_, i) =>
                Task.FromResult(i == 1 ? ActivityResult<string>.Success("ok") : ActivityResult<string>.Failure($"fan.code{i}", "refused")));
        }, "FanThAllMany_Orch");

        outcome.ErrorCode.ShouldBe("durable.fanout_failed");
        outcome.Total.ShouldBe(3);
        outcome.FailedCount.ShouldBe(2);
        outcome.Failures.ShouldBe(["0:fan.code0", "2:fan.code2"]);
    }

    [Fact]
    public async Task FanOutAllAsync_ActivityThrows_ReportsActivityFailedInTheAggregate()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThAllThrow_Orch",
                async (c, i) => ToOutcome(await c.FanOutAllAsync<int, string>("FanThAllThrow_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThAllThrow_Act", (_, i) =>
                i == 2 ? DurableTestHostSupport.ThrowAsync<ActivityResult<string>>() : Task.FromResult(ActivityResult<string>.Success("ok")));
        }, "FanThAllThrow_Orch");

        outcome.ErrorCode.ShouldBe("durable.fanout_failed");
        outcome.Failures.ShouldBe(["2:durable.activity_failed"]);
    }

    [Fact]
    public async Task FanOutAllAsync_PlainValueActivity_FailsClosedWithActivityFailed()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThPlain_Orch",
                async (c, i) => ToOutcome(await c.FanOutAllAsync<int, string>("FanThPlain_Act", i)));
            registry.AddActivityFunc<int, string>("FanThPlain_Act", (_, i) => Task.FromResult($"plain{i}"));
        }, "FanThPlain_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.fanout_failed");
        outcome.Failures.ShouldBe(["0:durable.activity_failed", "1:durable.activity_failed", "2:durable.activity_failed"]);
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_FastFailureAndSlowSuccess_ReturnsTheSuccess()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThFirst_Orch",
                async (c, i) => ToOutcome(await c.FanOutFirstSuccessAsync<int, string>("FanThFirst_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThFirst_Act", async (_, i) =>
            {
                if (i == 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(300));
                    return ActivityResult<string>.Success("slow");
                }

                return ActivityResult<string>.Failure($"fan.code{i}", "refused");
            });
        }, "FanThFirst_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Values.ShouldBe(["slow"]);
    }

    [Fact]
    public async Task FanOutFirstSuccessAsync_AllFail_ReturnsTheAggregateError()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThFirstFail_Orch",
                async (c, i) => ToOutcome(await c.FanOutFirstSuccessAsync<int, string>("FanThFirstFail_Act", i)));
            registry.AddActivityFunc<int, ActivityResult<string>>("FanThFirstFail_Act", (_, i) =>
                Task.FromResult(ActivityResult<string>.Failure($"fan.code{i}", "refused")));
        }, "FanThFirstFail_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.fanout_failed");
        outcome.Total.ShouldBe(3);
        outcome.FailedCount.ShouldBe(3);
        outcome.Failures.ShouldBe(["0:fan.code0", "1:fan.code1", "2:fan.code2"]);
    }

    [Fact]
    public async Task FanOutMultipleAsync_TwoActivities_UnwrapsEachResult()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThMulti_Orch", async (c, _) =>
            {
                var (first, second) = await c.FanOutMultipleAsync<string, string>(
                    ("FanThMulti_ActA", "a"), ("FanThMulti_ActB", "b"));
                return ToOutcome([first, second]);
            });
            registry.AddActivityFunc<object, ActivityResult<string>>("FanThMulti_ActA", (_, _) =>
                Task.FromResult(ActivityResult<string>.Success("fromA")));
            registry.AddActivityFunc<object, ActivityResult<string>>("FanThMulti_ActB", (_, _) =>
                Task.FromResult(ActivityResult<string>.Failure("fan.b_refused", "refused")));
        }, "FanThMulti_Orch");

        outcome.Values.ShouldBe(["fromA", "fan.b_refused"]);
    }

    [Fact]
    public async Task FanOutMultipleAsync_ThreeActivities_UnwrapsEachResult()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<List<int>, FanOutOutcome>("FanThMulti3_Orch", async (c, _) =>
            {
                var (first, second, third) = await c.FanOutMultipleAsync<string, string, string>(
                    ("FanThMulti3_ActA", "a"), ("FanThMulti3_ActB", "b"), ("FanThMulti3_ActC", "c"));
                return ToOutcome([first, second, third]);
            });
            registry.AddActivityFunc<object, ActivityResult<string>>("FanThMulti3_ActA", (_, _) =>
                Task.FromResult(ActivityResult<string>.Success("fromA")));
            registry.AddActivityFunc<object, ActivityResult<string>>("FanThMulti3_ActB", (_, _) =>
                Task.FromResult(ActivityResult<string>.Success("fromB")));
            registry.AddActivityFunc<object, ActivityResult<string>>("FanThMulti3_ActC", (_, _) =>
                Task.FromResult(ActivityResult<string>.Failure("fan.c_refused", "refused")));
        }, "FanThMulti3_Orch");

        outcome.Values.ShouldBe(["fromA", "fromB", "fan.c_refused"]);
    }
}

/// <summary>Serializable view of a fan-out result.</summary>
public sealed class FanOutOutcome
{
    public bool IsRight { get; set; }

    public List<string> Values { get; set; } = [];

    public string ErrorCode { get; set; } = string.Empty;

    public int Total { get; set; }

    public int FailedCount { get; set; }

    public List<string> Failures { get; set; } = [];
}
