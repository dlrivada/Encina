using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.DistributedLock;
using Encina.NBomber.Scenarios.Locking.Providers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Factory for creating SQL Server distributed lock load test scenarios.
/// Tests contention, release timing, and connection pool pressure using sp_getapplock.
/// </summary>
public sealed class SqlServerLockScenarioFactory
{
    private readonly LockScenarioContext _context;
    private IDistributedLockProvider? _lockProvider;
    private SqlServerLockProviderFactory? _sqlServerFactory;
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private readonly ConcurrentDictionary<string, int> _activeAcquisitions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerLockScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The lock scenario context.</param>
    public SqlServerLockScenarioFactory(LockScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _sqlServerFactory = context.ProviderFactory as SqlServerLockProviderFactory;
    }

    /// <summary>
    /// Creates all SQL Server lock scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateContentionScenario();
        yield return CreateReleaseTimingScenario();
        yield return CreateConnectionPressureScenario();
    }

    /// <summary>
    /// Creates the lock contention scenario.
    /// Tests sp_getapplock contention behavior with connection pool awareness.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateContentionScenario()
    {
        return Scenario.Create(
            name: "sqlserver-lock-contention",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    // Use limited bucket count for contention (5-10 resources)
                    var bucketId = _context.GetRandomBucket(scenarioContext.InvocationNumber);
                    var resource = _context.GetBucketResource(bucketId);

                    var sw = Stopwatch.StartNew();

                    // Try to acquire lock with moderate timeout
                    // Note: Connection pool is limited, so we use shorter wait times
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(10),
                        wait: TimeSpan.FromSeconds(3),
                        retry: TimeSpan.FromMilliseconds(100),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    sw.Stop();

                    if (lockHandle is not null)
                    {
                        // Track active acquisitions for mutual exclusion verification
                        var acquisitionCount = _activeAcquisitions.AddOrUpdate(resource, 1, (_, c) => c + 1);

                        if (acquisitionCount > 1)
                        {
                            _metrics.AddOrUpdate("mutual_exclusion_violations", 1, (_, c) => c + 1);
                        }

                        _metrics.AddOrUpdate("acquisitions", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("total_acquire_time_ms", sw.ElapsedMilliseconds, (_, c) => c + sw.ElapsedMilliseconds);

                        // Simulate work while holding the lock
                        await Task.Delay(20, CancellationToken.None).ConfigureAwait(false);

                        // Release
                        _activeAcquisitions.AddOrUpdate(resource, 0, (_, c) => Math.Max(0, c - 1));
                        await lockHandle.DisposeAsync().ConfigureAwait(false);

                        return Response.Ok(statusCode: "acquired");
                    }

                    _metrics.AddOrUpdate("timeouts", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "timeout");
                }
                catch (Exception ex)
                {
                    _metrics.AddOrUpdate("exceptions", 1, (_, c) => c + 1);
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
                var exceptions = _metrics.GetValueOrDefault("exceptions", 0);
                var totalAcquireTime = _metrics.GetValueOrDefault("total_acquire_time_ms", 0);
                var avgAcquireTime = acquisitions > 0 ? (double)totalAcquireTime / acquisitions : 0;

                Console.WriteLine($"SQL Server contention - Acquisitions: {acquisitions}, " +
                    $"Timeouts: {timeouts}, Violations: {violations}, Exceptions: {exceptions}, " +
                    $"Avg acquire time: {avgAcquireTime:F2}ms");

                _metrics.Clear();
                _activeAcquisitions.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                // Lower rate than Redis due to connection pool limitations
                Simulation.Inject(
                    rate: 50,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the release timing scenario.
    /// Measures session-scoped lock release latency.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateReleaseTimingScenario()
    {
        return Scenario.Create(
            name: "sqlserver-lock-release-timing",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    var resource = _context.NextResourceId();

                    // Acquire lock
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(30),
                        wait: TimeSpan.FromSeconds(5),
                        retry: TimeSpan.FromMilliseconds(100),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (lockHandle is null)
                    {
                        return Response.Fail("Failed to acquire lock", statusCode: "acquire_failed");
                    }

                    // Measure release time (sp_releaseapplock + connection close)
                    var sw = Stopwatch.StartNew();
                    await lockHandle.DisposeAsync().ConfigureAwait(false);
                    sw.Stop();

                    var releaseTimeMs = sw.ElapsedMilliseconds;
                    _metrics.AddOrUpdate("total_release_time_ms", releaseTimeMs, (_, c) => c + releaseTimeMs);
                    _metrics.AddOrUpdate("release_count", 1, (_, c) => c + 1);

                    if (releaseTimeMs < 10)
                    {
                        _metrics.AddOrUpdate("fast_releases", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "fast");
                    }
                    else if (releaseTimeMs < 50)
                    {
                        _metrics.AddOrUpdate("normal_releases", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "normal");
                    }
                    else
                    {
                        _metrics.AddOrUpdate("slow_releases", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "slow");
                    }
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
                var releaseCount = _metrics.GetValueOrDefault("release_count", 0);
                var totalReleaseTime = _metrics.GetValueOrDefault("total_release_time_ms", 0);
                var fastReleases = _metrics.GetValueOrDefault("fast_releases", 0);
                var normalReleases = _metrics.GetValueOrDefault("normal_releases", 0);
                var slowReleases = _metrics.GetValueOrDefault("slow_releases", 0);
                var avgReleaseTime = releaseCount > 0 ? (double)totalReleaseTime / releaseCount : 0;

                Console.WriteLine($"SQL Server release timing - " +
                    $"Fast (<10ms): {fastReleases}, Normal (10-50ms): {normalReleases}, Slow (>50ms): {slowReleases}, " +
                    $"Avg: {avgReleaseTime:F2}ms");

                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 30,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the connection pool pressure scenario.
    /// Tests lock behavior when connection pool is under stress.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConnectionPressureScenario()
    {
        return Scenario.Create(
            name: "sqlserver-lock-connection-pressure",
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

                    // Use very short timeout to quickly detect pool exhaustion
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(5),
                        wait: TimeSpan.FromMilliseconds(500),
                        retry: TimeSpan.FromMilliseconds(50),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    sw.Stop();

                    if (lockHandle is not null)
                    {
                        _metrics.AddOrUpdate("successful_acquisitions", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("total_acquire_time_ms", sw.ElapsedMilliseconds, (_, c) => c + sw.ElapsedMilliseconds);

                        // Hold lock briefly to create pressure
                        await Task.Delay(5, CancellationToken.None).ConfigureAwait(false);
                        await lockHandle.DisposeAsync().ConfigureAwait(false);

                        return Response.Ok(statusCode: "success");
                    }

                    _metrics.AddOrUpdate("pool_timeouts", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "pool_timeout");
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("pool", StringComparison.OrdinalIgnoreCase))
                {
                    // Connection pool exhausted
                    _metrics.AddOrUpdate("pool_exhaustion", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "pool_exhausted");
                }
                catch (Exception ex)
                {
                    _metrics.AddOrUpdate("exceptions", 1, (_, c) => c + 1);
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
                var successes = _metrics.GetValueOrDefault("successful_acquisitions", 0);
                var poolTimeouts = _metrics.GetValueOrDefault("pool_timeouts", 0);
                var poolExhaustion = _metrics.GetValueOrDefault("pool_exhaustion", 0);
                var exceptions = _metrics.GetValueOrDefault("exceptions", 0);
                var totalAcquireTime = _metrics.GetValueOrDefault("total_acquire_time_ms", 0);
                var avgAcquireTime = successes > 0 ? (double)totalAcquireTime / successes : 0;

                Console.WriteLine($"SQL Server connection pressure - " +
                    $"Successes: {successes}, Pool timeouts: {poolTimeouts}, Pool exhaustion: {poolExhaustion}, " +
                    $"Exceptions: {exceptions}, Avg acquire time: {avgAcquireTime:F2}ms");

                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                // High rate to stress connection pool
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
