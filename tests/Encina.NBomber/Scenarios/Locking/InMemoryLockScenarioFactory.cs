using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.DistributedLock;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Factory for creating in-memory lock load test scenarios.
/// Provides baseline performance for comparison with distributed providers.
/// </summary>
public sealed class InMemoryLockScenarioFactory
{
    private readonly LockScenarioContext _context;
    private IDistributedLockProvider? _lockProvider;
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private readonly ConcurrentDictionary<string, int> _activeAcquisitions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryLockScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The lock scenario context.</param>
    public InMemoryLockScenarioFactory(LockScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Creates all in-memory lock scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateContentionScenario();
        yield return CreateThroughputScenario();
    }

    /// <summary>
    /// Creates the lock contention scenario.
    /// Tests mutual exclusion with concurrent clients competing for limited resources.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateContentionScenario()
    {
        return Scenario.Create(
            name: "inmemory-lock-contention",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    var bucketId = _context.GetRandomBucket(scenarioContext.InvocationNumber);
                    var resource = _context.GetBucketResource(bucketId);

                    var sw = Stopwatch.StartNew();

                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(5),
                        wait: TimeSpan.FromSeconds(2),
                        retry: TimeSpan.FromMilliseconds(10),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    sw.Stop();

                    if (lockHandle is not null)
                    {
                        var acquisitionCount = _activeAcquisitions.AddOrUpdate(resource, 1, (_, c) => c + 1);

                        if (acquisitionCount > 1)
                        {
                            _metrics.AddOrUpdate("mutual_exclusion_violations", 1, (_, c) => c + 1);
                        }

                        _metrics.AddOrUpdate("acquisitions", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("total_acquire_time_ms", sw.ElapsedMilliseconds, (_, c) => c + sw.ElapsedMilliseconds);

                        // Simulate work
                        await Task.Delay(5, CancellationToken.None).ConfigureAwait(false);

                        _activeAcquisitions.AddOrUpdate(resource, 0, (_, c) => Math.Max(0, c - 1));
                        await lockHandle.DisposeAsync().ConfigureAwait(false);

                        return Response.Ok(statusCode: "acquired");
                    }

                    _metrics.AddOrUpdate("timeouts", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "timeout");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();
                return Task.CompletedTask;
            })
            .WithClean(_ =>
            {
                var acquisitions = _metrics.GetValueOrDefault("acquisitions", 0);
                var timeouts = _metrics.GetValueOrDefault("timeouts", 0);
                var violations = _metrics.GetValueOrDefault("mutual_exclusion_violations", 0);
                var totalAcquireTime = _metrics.GetValueOrDefault("total_acquire_time_ms", 0);
                var avgAcquireTime = acquisitions > 0 ? (double)totalAcquireTime / acquisitions : 0;

                Console.WriteLine($"InMemory contention - Acquisitions: {acquisitions}, " +
                    $"Timeouts: {timeouts}, Violations: {violations}, Avg acquire time: {avgAcquireTime:F2}ms");

                _metrics.Clear();
                _activeAcquisitions.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the throughput scenario.
    /// Measures maximum lock throughput without contention.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateThroughputScenario()
    {
        return Scenario.Create(
            name: "inmemory-lock-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    var resource = _context.NextResourceId();

                    var sw = Stopwatch.StartNew();

                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(1),
                        wait: TimeSpan.FromMilliseconds(100),
                        retry: TimeSpan.FromMilliseconds(10),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    sw.Stop();

                    if (lockHandle is not null)
                    {
                        _metrics.AddOrUpdate("acquisitions", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("total_time_ms", sw.ElapsedMilliseconds, (_, c) => c + sw.ElapsedMilliseconds);

                        await lockHandle.DisposeAsync().ConfigureAwait(false);
                        return Response.Ok(statusCode: "success");
                    }

                    _metrics.AddOrUpdate("failures", 1, (_, c) => c + 1);
                    return Response.Fail("Failed to acquire", statusCode: "failed");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();
                return Task.CompletedTask;
            })
            .WithClean(_ =>
            {
                var acquisitions = _metrics.GetValueOrDefault("acquisitions", 0);
                var failures = _metrics.GetValueOrDefault("failures", 0);
                var totalTime = _metrics.GetValueOrDefault("total_time_ms", 0);
                var avgTime = acquisitions > 0 ? (double)totalTime / acquisitions : 0;

                Console.WriteLine($"InMemory throughput - Acquisitions: {acquisitions}, " +
                    $"Failures: {failures}, Avg time: {avgTime:F2}ms");

                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 500,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
