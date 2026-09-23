using System.Diagnostics;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Hangfire;

/// <summary>
/// End-to-end check of the Hangfire failure semantics (#1152, #1159 review) on a real
/// <see cref="BackgroundJobServer"/> with in-memory storage: with
/// <see cref="EncinaAutomaticRetry.UseEncinaAutomaticRetry"/>, a permanent Encina failure leaves the
/// job Failed without retries, while a transient failure is scheduled for a retry.
/// </summary>
/// <remarks>
/// The test uses its own storage, filter collection and activator, never Hangfire's global
/// configuration, so it cannot interfere with other tests.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Scheduler", "Hangfire")]
public sealed class HangfireRetryPolicyIntegrationTests
{
    [Fact]
    public async Task PermanentFailure_IsNotRetried_JobEndsFailed()
    {
        var state = await RunJobAndWaitForStateAsync("order.validation_failed");

        state.ShouldBe(FailedState.StateName);
    }

    [Fact]
    public async Task TransientFailure_IsScheduledForRetry()
    {
        var state = await RunJobAndWaitForStateAsync("store.timeout");

        state.ShouldBe(ScheduledState.StateName);
    }

    [Fact]
    public async Task Success_JobEndsSucceeded()
    {
        var state = await RunJobAndWaitForStateAsync(string.Empty);

        state.ShouldBe(SucceededState.StateName);
    }

    private static async Task<string?> RunJobAndWaitForStateAsync(string errorCode)
    {
        var storage = new InMemoryStorage();
        var filters = new JobFilterCollection().UseEncinaAutomaticRetry(attempts: 3);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddTransient<IRequestHandler<RetryProbeRequest, string>, RetryProbeHandler>();
        services.AddEncinaHangfire(options => options.ProviderHealthCheck.Enabled = false);
        await using var provider = services.BuildServiceProvider();

        using var server = new BackgroundJobServer(
            new BackgroundJobServerOptions
            {
                ServerName = $"retry-policy-{Guid.NewGuid():N}",
                WorkerCount = 1,
                FilterProvider = filters,
                Activator = new ServiceProviderJobActivator(provider)
            },
            storage);

        var client = new BackgroundJobClient(storage, filters);
        var jobId = client.Enqueue<HangfireRequestJobAdapter<RetryProbeRequest, string>>(
            adapter => adapter.ExecuteAsync(new RetryProbeRequest(errorCode), default));

        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(20))
        {
            using var connection = storage.GetConnection();
            var state = connection.GetStateData(jobId)?.Name;
            if (state == FailedState.StateName || state == ScheduledState.StateName || state == SucceededState.StateName)
            {
                return state;
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Hangfire job {jobId} did not reach a final state within 20 s.");
    }

    public sealed record RetryProbeRequest(string ErrorCode) : IRequest<string>;

    public sealed class RetryProbeHandler : IRequestHandler<RetryProbeRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(RetryProbeRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(string.IsNullOrEmpty(request.ErrorCode)
                ? Right<EncinaError, string>("ok")
                : Left<EncinaError, string>(EncinaErrors.Create(request.ErrorCode, "Probe failure")));
    }

    private sealed class ServiceProviderJobActivator(IServiceProvider provider) : JobActivator
    {
        public override JobActivatorScope BeginScope(JobActivatorContext context) =>
            new Scope(provider.CreateScope());

        private sealed class Scope(IServiceScope scope) : JobActivatorScope
        {
            public override object Resolve(Type type) => scope.ServiceProvider.GetRequiredService(type);

            public override void DisposeScope() => scope.Dispose();
        }
    }
}
