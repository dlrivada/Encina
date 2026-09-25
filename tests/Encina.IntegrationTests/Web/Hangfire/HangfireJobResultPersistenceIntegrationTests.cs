using System.Diagnostics;
using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Hangfire.States;
using Hangfire.Storage;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.IntegrationTests.Web.Hangfire;

/// <summary>
/// Runs <see cref="HangfireRequestJobAdapter{TRequest, TResponse}"/> on a real
/// <see cref="BackgroundJobServer"/> with in-memory storage and inspects what Hangfire persisted
/// for the succeeded job (#1173, #1259 review): the default entry point
/// (<c>EnqueueRequest</c> → <c>ExecuteAsync</c>) stores no result, while the explicit opt-in
/// (<c>EnqueueRequestWithResult</c> → <c>ExecuteAndReturnResultAsync</c>) stores the response.
/// </summary>
/// <remarks>
/// The test uses its own storage, filter collection and activator, never Hangfire's global
/// configuration, so it cannot interfere with other tests.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Scheduler", "Hangfire")]
public sealed class HangfireJobResultPersistenceIntegrationTests
{
    private const string SensitiveMarker = "F41.1-patient-123";

    [Fact]
    public async Task EnqueueRequest_Default_DoesNotPersistTheResponse()
    {
        // Act
        var (state, stateData) = await RunSucceededJobAsync(
            (client, request) => client.EnqueueRequest<DiagnosisLookupRequest, string>(request));

        // Assert
        state.ShouldBe(SucceededState.StateName);
        stateData.TryGetValue("Result", out var result).ShouldBeFalse(
            $"Hangfire stored a job result: {result}");
        stateData.Values.ShouldAllBe(v => v == null || !v.Contains(SensitiveMarker));
    }

    [Fact]
    public async Task EnqueueRequestWithResult_OptIn_PersistsTheResponse()
    {
        // Act
        var (state, stateData) = await RunSucceededJobAsync(
            (client, request) => client.EnqueueRequestWithResult<DiagnosisLookupRequest, string>(request));

        // Assert
        state.ShouldBe(SucceededState.StateName);
        stateData.ShouldContainKey("Result");
        stateData["Result"].ShouldContain(SensitiveMarker);
    }

    private static async Task<(string? State, IDictionary<string, string> Data)> RunSucceededJobAsync(
        Func<IBackgroundJobClient, DiagnosisLookupRequest, string> enqueue)
    {
        var storage = new InMemoryStorage();
        var filters = new JobFilterCollection();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncina();
        services.AddTransient<IRequestHandler<DiagnosisLookupRequest, string>, DiagnosisLookupHandler>();
        services.AddEncinaHangfire(options => options.ProviderHealthCheck.Enabled = false);
        await using var provider = services.BuildServiceProvider();

        using var server = new BackgroundJobServer(
            new BackgroundJobServerOptions
            {
                ServerName = $"result-persistence-{Guid.NewGuid():N}",
                WorkerCount = 1,
                FilterProvider = filters,
                Activator = new ServiceProviderJobActivator(provider)
            },
            storage);

        var client = new BackgroundJobClient(storage, filters);
        var jobId = enqueue(client, new DiagnosisLookupRequest("patient-123"));

        var deadline = Stopwatch.StartNew();
        while (deadline.Elapsed < TimeSpan.FromSeconds(20))
        {
            using var connection = storage.GetConnection();
            StateData? state = connection.GetStateData(jobId);
            if (state is not null && (state.Name == SucceededState.StateName || state.Name == FailedState.StateName))
            {
                return (state.Name, state.Data);
            }

            await Task.Delay(50);
        }

        throw new TimeoutException($"Hangfire job {jobId} did not reach a final state within 20 s.");
    }

    public sealed record DiagnosisLookupRequest(string SubjectId) : IRequest<string>;

    public sealed class DiagnosisLookupHandler : IRequestHandler<DiagnosisLookupRequest, string>
    {
        public Task<Either<EncinaError, string>> Handle(DiagnosisLookupRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(Right<EncinaError, string>($"F41.1-{request.SubjectId}"));
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
