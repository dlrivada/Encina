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

    /// <summary>Attempts to start a host before an "address already in use" is reported (the port is random).</summary>
    private const int StartAttempts = 5;

    /// <summary>
    /// Starts a host. The test host picks a random port and binds it afterwards, so on a loaded runner the
    /// port can be taken in between; only that start-up race is retried, never a test failure.
    /// </summary>
    internal static async Task<DurableTaskTestHost> StartAsync(Action<DurableTaskRegistry> register)
    {
        var options = new DurableTaskTestHostOptions
        {
            LoggerFactory = NullLoggerFactory.Instance
        };

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await DurableTaskTestHost.StartAsync(register, options);
            }
            catch (IOException) when (attempt < StartAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
            }
        }
    }

    /// <summary>
    /// Waits until the orchestration reports the custom status <c>waiting</c>. The orchestrator sets it right
    /// before it suspends on the awaited event, so once it is visible the event subscription exists and a
    /// raised event cannot be lost to a slow start.
    /// </summary>
    internal static async Task WaitUntilWaitingAsync(DurableTaskTestHost host, string instanceId, CancellationToken cancellationToken)
    {
        while (true)
        {
            var metadata = await host.Client.GetInstanceAsync(instanceId, getInputsAndOutputs: true, cancellationToken);
            if (metadata is not null
                && (metadata.IsCompleted || metadata.ReadCustomStatusAs<string>() == WaitingStatus))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }
    }

    internal const string WaitingStatus = "waiting";

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
