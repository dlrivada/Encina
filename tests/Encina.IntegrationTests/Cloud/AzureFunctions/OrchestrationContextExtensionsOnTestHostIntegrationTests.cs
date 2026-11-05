using Encina.AzureFunctions.Durable;
using LanguageExt;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Testing;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Runs the <see cref="OrchestrationContextExtensions"/> helpers inside orchestrations on the Durable Task
/// in-process test host. One behaviour per orchestrator; error codes are asserted, never messages.
/// Orchestrator names are literals at each registration: the Durable analyzers crash on a constant or a
/// variable name.
/// </summary>
[Collection("Durable-TestHost")]
[Trait("Category", "Integration")]
[Trait("Cloud", "AzureFunctions")]
public sealed class OrchestrationContextExtensionsOnTestHostIntegrationTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(1);

    private static DurableEitherOutcome ToOutcome(Either<EncinaError, string> result)
        => result.Match(
            Right: value => new DurableEitherOutcome { IsRight = true, Value = value },
            Left: error => new DurableEitherOutcome { ErrorCode = error.GetCode().IfNone(string.Empty) });

    private static async Task<DurableEitherOutcome> RunAsync(
        Action<DurableTaskRegistry> register,
        string orchestratorName)
    {
        await using var host = await DurableTestHostSupport.StartAsync(register);
        var metadata = await DurableTestHostSupport.RunAsync(host, orchestratorName, "in");

        metadata.RuntimeStatus.ShouldBe(OrchestrationRuntimeStatus.Completed);
        return metadata.ReadOutputAs<DurableEitherOutcome>()!;
    }

    private static async Task<DurableEitherOutcome> RaiseAsync(
        Action<DurableTaskRegistry> register,
        string orchestratorName)
    {
        await using var host = await DurableTestHostSupport.StartAsync(register);
        using var cts = new CancellationTokenSource(DurableTestHostSupport.RunTimeout);
        var id = await host.Client.ScheduleNewOrchestrationInstanceAsync(orchestratorName, "in", cts.Token);
        await host.Client.WaitForInstanceStartAsync(id, cts.Token);

        await host.Client.RaiseEventAsync(id, "go", "payload", cts.Token);
        var metadata = await host.Client.WaitForInstanceCompletionAsync(id, getInputsAndOutputs: true, cts.Token);

        return metadata.ReadOutputAs<DurableEitherOutcome>()!;
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_ActivityReturnsSuccess_ReturnsRightWithTheValue()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThWrOk_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityWithResultAsync<string, string>("ExtThWrOk_Act", i)));
            registry.AddActivityFunc<string, ActivityResult<string>>(
                "ExtThWrOk_Act", (_, i) => Task.FromResult(ActivityResult<string>.Success(i + "-done")));
        }, "ExtThWrOk_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("in-done");
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_ActivityReturnsFailure_ReturnsLeftWithTheActivityCode()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThWrFail_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityWithResultAsync<string, string>("ExtThWrFail_Act", i)));
            registry.AddActivityFunc<string, ActivityResult<string>>(
                "ExtThWrFail_Act", (_, _) => Task.FromResult(ActivityResult<string>.Failure("ext.refused", "refused")));
        }, "ExtThWrFail_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("ext.refused");
    }

    [Fact]
    public async Task CallEncinaActivityWithResultAsync_ActivityThrows_ReturnsActivityFailedError()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThWrThrow_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityWithResultAsync<string, string>("ExtThWrThrow_Act", i)));
            registry.AddActivityFunc<string, ActivityResult<string>>(
                "ExtThWrThrow_Act", (_, _) => DurableTestHostSupport.ThrowAsync<ActivityResult<string>>());
        }, "ExtThWrThrow_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.activity_failed");
    }

    [Fact]
    public async Task CallEncinaActivityAsync_ActivityReturnsAValue_ReturnsRightWithTheValue()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThPlainOk_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityAsync<string, string>("ExtThPlainOk_Act", i)));
            registry.AddActivityFunc<string, string>("ExtThPlainOk_Act", (_, i) => Task.FromResult(i + "-plain"));
        }, "ExtThPlainOk_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("in-plain");
    }

    [Fact]
    public async Task CallEncinaActivityAsync_ActivityThrows_ReturnsActivityFailedError()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThPlainThrow_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityAsync<string, string>("ExtThPlainThrow_Act", i)));
            registry.AddActivityFunc<string, string>("ExtThPlainThrow_Act", (_, _) => DurableTestHostSupport.ThrowAsync<string>());
        }, "ExtThPlainThrow_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.activity_failed");
    }

    [Fact]
    public async Task CallEncinaActivityAsync_WithCreateRetryOptions_RetriesUntilTheActivitySucceeds()
    {
        var attempts = 0;
        var retry = OrchestrationContextExtensions.CreateRetryOptions(3, TimeSpan.FromMilliseconds(100), 1.0);

        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThRetry_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaActivityAsync<string, string>("ExtThRetry_Act", i, retry)));
            registry.AddActivityFunc<string, string>("ExtThRetry_Act", (_, i) =>
            {
                if (Interlocked.Increment(ref attempts) < 3)
                {
                    return DurableTestHostSupport.ThrowAsync<string>();
                }

                return Task.FromResult(i + "-retried");
            });
        }, "ExtThRetry_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("in-retried");
        attempts.ShouldBe(3);
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_SubOrchestratorReturnsAValue_ReturnsRightWithTheValue()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThSubOk_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaSubOrchestratorAsync<string, string>("ExtThSubOk_Sub", i)));
            registry.AddOrchestratorFunc<string, string>("ExtThSubOk_Sub", (_, i) => Task.FromResult(i + "-sub"));
        }, "ExtThSubOk_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("in-sub");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorAsync_SubOrchestratorThrows_ReturnsSubOrchestratorFailedError()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThSubThrow_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaSubOrchestratorAsync<string, string>("ExtThSubThrow_Sub", i)));
            registry.AddOrchestratorFunc<string, string>("ExtThSubThrow_Sub", (_, _) => DurableTestHostSupport.ThrowAsync<string>());
        }, "ExtThSubThrow_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.suborchestrator_failed");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_SubOrchestratorReturnsSuccess_ReturnsRight()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThSubWrOk_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaSubOrchestratorWithResultAsync<string, string>("ExtThSubWrOk_Sub", i)));
            registry.AddOrchestratorFunc<string, ActivityResult<string>>(
                "ExtThSubWrOk_Sub", (_, i) => Task.FromResult(ActivityResult<string>.Success(i + "-sub")));
        }, "ExtThSubWrOk_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("in-sub");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_SubOrchestratorReturnsFailure_ReturnsLeftWithTheCode()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThSubWrFail_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaSubOrchestratorWithResultAsync<string, string>("ExtThSubWrFail_Sub", i)));
            registry.AddOrchestratorFunc<string, ActivityResult<string>>(
                "ExtThSubWrFail_Sub", (_, _) => Task.FromResult(ActivityResult<string>.Failure("ext.sub_refused", "refused")));
        }, "ExtThSubWrFail_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("ext.sub_refused");
    }

    [Fact]
    public async Task CallEncinaSubOrchestratorWithResultAsync_SubOrchestratorThrows_ReturnsSubOrchestratorFailedError()
    {
        var outcome = await RunAsync(registry =>
        {
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThSubWrThrow_Orch",
                async (c, i) => ToOutcome(await c.CallEncinaSubOrchestratorWithResultAsync<string, string>("ExtThSubWrThrow_Sub", i)));
            registry.AddOrchestratorFunc<string, ActivityResult<string>>(
                "ExtThSubWrThrow_Sub", (_, _) => DurableTestHostSupport.ThrowAsync<ActivityResult<string>>());
        }, "ExtThSubWrThrow_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.suborchestrator_failed");
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_TimeoutElapses_ReturnsEventTimeoutError()
    {
        var outcome = await RunAsync(registry =>
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThEvTimeout_Orch",
                async (c, _) => ToOutcome(await c.WaitForEncinaExternalEventAsync<string>("never", EventTimeout))),
            "ExtThEvTimeout_Orch");

        outcome.IsRight.ShouldBeFalse();
        outcome.ErrorCode.ShouldBe("durable.event_timeout");
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_EventArrivesBeforeTheTimeout_ReturnsRightWithThePayload()
    {
        var outcome = await RaiseAsync(registry =>
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThEvFirst_Orch",
                async (c, _) => ToOutcome(await c.WaitForEncinaExternalEventAsync<string>("go", TimeSpan.FromSeconds(2)))),
            "ExtThEvFirst_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("payload");
    }

    [Fact]
    public async Task WaitForEncinaExternalEventAsync_WithoutTimeout_ReturnsRightWithThePayload()
    {
        var outcome = await RaiseAsync(registry =>
            registry.AddOrchestratorFunc<string, DurableEitherOutcome>("ExtThEvPlain_Orch",
                async (c, _) => ToOutcome(await c.WaitForEncinaExternalEventAsync<string>("go"))),
            "ExtThEvPlain_Orch");

        outcome.IsRight.ShouldBeTrue();
        outcome.Value.ShouldBe("payload");
    }

    [Fact]
    public async Task GetCorrelationId_InsideAnOrchestration_ReturnsTheInstanceId()
    {
        await using var host = await DurableTestHostSupport.StartAsync(registry =>
            registry.AddOrchestratorFunc<string, string>(
                "ExtThCorr_Orch", (c, _) => Task.FromResult(c.GetCorrelationId())));
        using var cts = new CancellationTokenSource(DurableTestHostSupport.RunTimeout);
        var id = await host.Client.ScheduleNewOrchestrationInstanceAsync("ExtThCorr_Orch", "in", cts.Token);

        var metadata = await host.Client.WaitForInstanceCompletionAsync(id, getInputsAndOutputs: true, cts.Token);

        metadata.ReadOutputAs<string>().ShouldBe(id);
    }
}
