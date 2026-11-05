using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.IntegrationTests.Cloud.AzureFunctions;

/// <summary>
/// Shared plumbing for the tests that run orchestrations on the Durable Task in-process test host.
/// Orchestrator and activity names must be unique across the whole assembly (analyzer AD0001), so each
/// test class prefixes its own.
/// </summary>
internal static class DurableTestHostSupport
{
    internal static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(60);

    internal static Task<DurableTaskTestHost> StartAsync(Action<DurableTaskRegistry> register)
    {
        var options = new DurableTaskTestHostOptions
        {
            LoggerFactory = NullLoggerFactory.Instance
        };

        return DurableTaskTestHost.StartAsync(register, options);
    }

    /// <summary>Async body that always throws, so a registered function fails inside the engine.</summary>
    internal static async Task<T> ThrowAsync<T>()
    {
        await Task.Yield();
        throw new InvalidOperationException("planned failure");
    }

    /// <summary>
    /// Schedules the orchestrator, waits for it with a bounded token and returns its metadata.
    /// </summary>
    internal static async Task<OrchestrationMetadata> RunAsync<TInput>(
        DurableTaskTestHost host,
        string orchestratorName,
        TInput input)
    {
        using var cts = new CancellationTokenSource(RunTimeout);
        var instanceId = await host.Client.ScheduleNewOrchestrationInstanceAsync(
            orchestratorName, input, cts.Token);

        return await host.Client.WaitForInstanceCompletionAsync(instanceId, getInputsAndOutputs: true, cts.Token);
    }
}

/// <summary>Serializes the Durable test-host classes: each host listens on a random port.</summary>
[CollectionDefinition("Durable-TestHost", DisableParallelization = true)]
public sealed class DurableTestHostSerialization;

/// <summary>Serializable view of a saga run (the saga error type itself is not serializable).</summary>
public sealed class DurableSagaOutcome
{
    public bool Succeeded { get; set; }

    public string Trail { get; set; } = string.Empty;

    public string FailedStep { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public bool WasCompensated { get; set; }

    public List<string> CompensationFailures { get; set; } = [];

    public List<string> CompensatedSteps { get; set; } = [];
}

/// <summary>Saga data that flows through the steps of the saga tests.</summary>
public sealed class DurableSagaTestData
{
    public string Trail { get; set; } = string.Empty;
}

/// <summary>Serializable view of an Either returned by an Encina helper.</summary>
public sealed class DurableEitherOutcome
{
    public bool IsRight { get; set; }

    public string Value { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;
}
