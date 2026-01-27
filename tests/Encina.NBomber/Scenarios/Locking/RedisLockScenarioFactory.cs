using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.DistributedLock;
using Encina.NBomber.Scenarios.Locking.Providers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Factory for creating Redis distributed lock load test scenarios.
/// Tests contention, release timing, renewal, and timeout accuracy.
/// </summary>
public sealed class RedisLockScenarioFactory
{
    private readonly LockScenarioContext _context;
    private IDistributedLockProvider? _lockProvider;
    private RedisLockProviderFactory? _redisFactory;
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private readonly ConcurrentDictionary<string, int> _activeAcquisitions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisLockScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The lock scenario context.</param>
    public RedisLockScenarioFactory(LockScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _redisFactory = context.ProviderFactory as RedisLockProviderFactory;
    }

    /// <summary>
    /// Creates all Redis lock scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateContentionScenario();
        yield return CreateReleaseTimingScenario();
        yield return CreateRenewalScenario();
        yield return CreateTimeoutAccuracyScenario();
    }

    /// <summary>
    /// Creates the lock contention scenario.
    /// Tests mutual exclusion with 50+ concurrent clients competing for 5-10 resources.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateContentionScenario()
    {
        return Scenario.Create(
            name: $"redis-lock-contention-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    // Use limited bucket count for high contention (5-10 resources)
                    var bucketId = _context.GetRandomBucket(scenarioContext.InvocationNumber);
                    var resource = _context.GetBucketResource(bucketId);

                    var sw = Stopwatch.StartNew();

                    // Try to acquire lock with short timeout
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(5),
                        wait: TimeSpan.FromSeconds(2),
                        retry: TimeSpan.FromMilliseconds(50),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    sw.Stop();

                    if (lockHandle is not null)
                    {
                        // Track that we acquired the lock for this resource
                        var acquisitionCount = _activeAcquisitions.AddOrUpdate(resource, 1, (_, c) => c + 1);

                        // Verify mutual exclusion: only one acquisition should be active
                        if (acquisitionCount > 1)
                        {
                            _metrics.AddOrUpdate("mutual_exclusion_violations", 1, (_, c) => c + 1);
                        }

                        _metrics.AddOrUpdate("acquisitions", 1, (_, c) => c + 1);
                        _metrics.AddOrUpdate("total_acquire_time_ms", sw.ElapsedMilliseconds, (_, c) => c + sw.ElapsedMilliseconds);

                        // Simulate some work while holding the lock
                        await Task.Delay(10, CancellationToken.None).ConfigureAwait(false);

                        // Release the lock
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
            .WithInit(async _ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();

                if (_redisFactory is not null)
                {
                    await _redisFactory.FlushDatabaseAsync().ConfigureAwait(false);
                }
            })
            .WithClean(_ =>
            {
                var acquisitions = _metrics.GetValueOrDefault("acquisitions", 0);
                var timeouts = _metrics.GetValueOrDefault("timeouts", 0);
                var violations = _metrics.GetValueOrDefault("mutual_exclusion_violations", 0);
                var totalAcquireTime = _metrics.GetValueOrDefault("total_acquire_time_ms", 0);
                var avgAcquireTime = acquisitions > 0 ? (double)totalAcquireTime / acquisitions : 0;

                Console.WriteLine($"Redis contention ({_context.ProviderName}) - Acquisitions: {acquisitions}, " +
                    $"Timeouts: {timeouts}, Violations: {violations}, Avg acquire time: {avgAcquireTime:F2}ms");

                _metrics.Clear();
                _activeAcquisitions.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the release timing scenario.
    /// Measures lock release latency under load. Target: &lt;10ms release time.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateReleaseTimingScenario()
    {
        return Scenario.Create(
            name: $"redis-lock-release-timing-{_context.ProviderName}",
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
                        expiry: TimeSpan.FromSeconds(10),
                        wait: TimeSpan.FromSeconds(5),
                        retry: TimeSpan.FromMilliseconds(50),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (lockHandle is null)
                    {
                        return Response.Fail("Failed to acquire lock", statusCode: "acquire_failed");
                    }

                    // Measure release time
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
            .WithInit(async _ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();

                if (_redisFactory is not null)
                {
                    await _redisFactory.FlushDatabaseAsync().ConfigureAwait(false);
                }
            })
            .WithClean(_ =>
            {
                var releaseCount = _metrics.GetValueOrDefault("release_count", 0);
                var totalReleaseTime = _metrics.GetValueOrDefault("total_release_time_ms", 0);
                var fastReleases = _metrics.GetValueOrDefault("fast_releases", 0);
                var normalReleases = _metrics.GetValueOrDefault("normal_releases", 0);
                var slowReleases = _metrics.GetValueOrDefault("slow_releases", 0);
                var avgReleaseTime = releaseCount > 0 ? (double)totalReleaseTime / releaseCount : 0;

                Console.WriteLine($"Redis release timing ({_context.ProviderName}) - " +
                    $"Fast (<10ms): {fastReleases}, Normal (10-50ms): {normalReleases}, Slow (>50ms): {slowReleases}, " +
                    $"Avg: {avgReleaseTime:F2}ms");

                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the lock renewal scenario.
    /// Tests lock extension under contention.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateRenewalScenario()
    {
        return Scenario.Create(
            name: $"redis-lock-renewal-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null)
                    {
                        return Response.Fail("Lock provider not initialized", statusCode: "no_provider");
                    }

                    // Use shared resources for contention during renewal
                    var bucketId = _context.GetRandomBucket(scenarioContext.InvocationNumber);
                    var resource = _context.GetBucketResource(bucketId);

                    // Acquire lock with short expiry
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: TimeSpan.FromSeconds(2),
                        wait: TimeSpan.FromSeconds(3),
                        retry: TimeSpan.FromMilliseconds(50),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (lockHandle is null)
                    {
                        _metrics.AddOrUpdate("acquire_failures", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: "acquire_timeout");
                    }

                    _metrics.AddOrUpdate("acquisitions", 1, (_, c) => c + 1);

                    // Simulate work, then extend the lock
                    await Task.Delay(500, CancellationToken.None).ConfigureAwait(false);

                    // Try to extend the lock
                    var extended = await _lockProvider.ExtendAsync(
                        resource,
                        extension: TimeSpan.FromSeconds(5),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (extended)
                    {
                        _metrics.AddOrUpdate("successful_renewals", 1, (_, c) => c + 1);

                        // Do more work after renewal
                        await Task.Delay(200, CancellationToken.None).ConfigureAwait(false);
                    }
                    else
                    {
                        _metrics.AddOrUpdate("failed_renewals", 1, (_, c) => c + 1);
                    }

                    await lockHandle.DisposeAsync().ConfigureAwait(false);
                    return Response.Ok(statusCode: extended ? "renewed" : "renewal_failed");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();

                if (_redisFactory is not null)
                {
                    await _redisFactory.FlushDatabaseAsync().ConfigureAwait(false);
                }
            })
            .WithClean(_ =>
            {
                var acquisitions = _metrics.GetValueOrDefault("acquisitions", 0);
                var acquireFailures = _metrics.GetValueOrDefault("acquire_failures", 0);
                var successfulRenewals = _metrics.GetValueOrDefault("successful_renewals", 0);
                var failedRenewals = _metrics.GetValueOrDefault("failed_renewals", 0);

                Console.WriteLine($"Redis renewal ({_context.ProviderName}) - Acquisitions: {acquisitions}, " +
                    $"Acquire failures: {acquireFailures}, Successful renewals: {successfulRenewals}, " +
                    $"Failed renewals: {failedRenewals}");

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
    /// Creates the timeout accuracy scenario.
    /// Tests lock expiration timing accuracy under load.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateTimeoutAccuracyScenario()
    {
        return Scenario.Create(
            name: $"redis-lock-timeout-accuracy-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    if (_lockProvider is null || _redisFactory?.GetDatabase() is null)
                    {
                        return Response.Fail("Provider not initialized", statusCode: "no_provider");
                    }

                    var database = _redisFactory.GetDatabase()!;
                    var resource = _context.NextResourceId();
                    var lockExpiry = TimeSpan.FromSeconds(3);

                    // Acquire lock
                    var acquireTime = Stopwatch.GetTimestamp();
                    var lockHandle = await _lockProvider.TryAcquireAsync(
                        resource,
                        expiry: lockExpiry,
                        wait: TimeSpan.FromSeconds(1),
                        retry: TimeSpan.FromMilliseconds(50),
                        cancellationToken: CancellationToken.None).ConfigureAwait(false);

                    if (lockHandle is null)
                    {
                        return Response.Fail("Failed to acquire lock", statusCode: "acquire_failed");
                    }

                    // Check TTL immediately after acquisition
                    var lockKey = $"{_context.Options.KeyPrefix}:{resource}";
                    var ttl = await database.KeyTimeToLiveAsync(lockKey).ConfigureAwait(false);

                    // Release the lock immediately
                    await lockHandle.DisposeAsync().ConfigureAwait(false);

                    if (ttl.HasValue)
                    {
                        var expectedTtlMs = lockExpiry.TotalMilliseconds;
                        var actualTtlMs = ttl.Value.TotalMilliseconds;
                        var driftMs = Math.Abs(expectedTtlMs - actualTtlMs);

                        _metrics.AddOrUpdate("total_drift_ms", (long)driftMs, (_, c) => c + (long)driftMs);
                        _metrics.AddOrUpdate("ttl_checks", 1, (_, c) => c + 1);

                        if (driftMs < 100)
                        {
                            _metrics.AddOrUpdate("accurate", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "accurate");
                        }
                        else if (driftMs < 500)
                        {
                            _metrics.AddOrUpdate("minor_drift", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "minor_drift");
                        }
                        else
                        {
                            _metrics.AddOrUpdate("major_drift", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "major_drift");
                        }
                    }

                    return Response.Fail("TTL not available", statusCode: "no_ttl");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                _lockProvider = _context.ProviderFactory.CreateLockProvider();

                if (_redisFactory is not null)
                {
                    await _redisFactory.FlushDatabaseAsync().ConfigureAwait(false);
                }
            })
            .WithClean(_ =>
            {
                var totalDrift = _metrics.GetValueOrDefault("total_drift_ms", 0);
                var checks = _metrics.GetValueOrDefault("ttl_checks", 0);
                var accurate = _metrics.GetValueOrDefault("accurate", 0);
                var minorDrift = _metrics.GetValueOrDefault("minor_drift", 0);
                var majorDrift = _metrics.GetValueOrDefault("major_drift", 0);
                var avgDrift = checks > 0 ? (double)totalDrift / checks : 0;

                Console.WriteLine($"Redis timeout accuracy ({_context.ProviderName}) - " +
                    $"Accurate (<100ms): {accurate}, Minor drift (100-500ms): {minorDrift}, Major drift (>500ms): {majorDrift}, " +
                    $"Avg drift: {avgDrift:F2}ms");

                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 50,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
