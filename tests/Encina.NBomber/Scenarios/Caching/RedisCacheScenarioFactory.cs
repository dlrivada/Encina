using System.Collections.Concurrent;
using System.Diagnostics;
using Encina.Caching;
using Encina.NBomber.Scenarios.Caching.Providers;
using NBomber.Contracts;
using NBomber.CSharp;
using StackExchange.Redis;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Factory for creating Redis cache load test scenarios.
/// Tests throughput, concurrent access, expiration accuracy, and pipelining.
/// </summary>
public sealed class RedisCacheScenarioFactory
{
    private readonly CacheScenarioContext _context;
    private readonly ICacheProvider _cacheProvider;
    private readonly RedisCacheProviderFactory? _redisFactory;
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private bool _databaseFlushed;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisCacheScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The cache scenario context.</param>
    public RedisCacheScenarioFactory(CacheScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _redisFactory = context.ProviderFactory as RedisCacheProviderFactory;
        // Create cache provider once - shared across all scenarios
        _cacheProvider = _context.ProviderFactory.CreateCacheProvider();
    }

    /// <summary>
    /// Creates all Redis cache scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateGetSetThroughputScenario();
        yield return CreateConcurrentAccessScenario();
        yield return CreateExpirationAccuracyScenario();
        yield return CreatePipelineBatchingScenario();
    }

    /// <summary>
    /// Creates the get/set throughput scenario.
    /// Tests read/write throughput with connection multiplexer pooling.
    /// Target: 5,000+ ops/sec.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateGetSetThroughputScenario()
    {
        return Scenario.Create(
            name: $"redis-get-set-throughput-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    var key = _context.NextCacheKey();
                    var isWrite = scenarioContext.InvocationNumber % 3 == 0; // 33% writes

                    if (isWrite)
                    {
                        var value = _context.CreateTestData();
                        await _cacheProvider.SetAsync(key, value, _context.Options.DefaultExpiration, CancellationToken.None)
                            .ConfigureAwait(false);
                        return Response.Ok(statusCode: "write");
                    }
                    else
                    {
                        // Read from a recent key
                        var readKey = scenarioContext.InvocationNumber > 10
                            ? $"{_context.Options.KeyPrefix}:key:{scenarioContext.InvocationNumber - 5}"
                            : key;

                        var value = await _cacheProvider.GetAsync<string>(readKey, CancellationToken.None)
                            .ConfigureAwait(false);

                        _metrics.AddOrUpdate(value is not null ? "hits" : "misses", 1, (_, c) => c + 1);
                        return Response.Ok(statusCode: value is not null ? "read:hit" : "read:miss");
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                // Flush database before test (only once)
                if (!_databaseFlushed && _redisFactory is not null)
                {
                    await _redisFactory.FlushDatabaseAsync().ConfigureAwait(false);
                    _databaseFlushed = true;
                }
            })
            .WithClean(_ =>
            {
                var hits = _metrics.GetValueOrDefault("hits", 0);
                var misses = _metrics.GetValueOrDefault("misses", 0);
                Console.WriteLine($"Redis throughput ({_context.ProviderName}) - Hits: {hits}, Misses: {misses}");
                _metrics.Clear();
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
    /// Creates the concurrent access scenario.
    /// Tests Lua script atomicity under concurrent load.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConcurrentAccessScenario()
    {
        const int bucketCount = 10;

        return Scenario.Create(
            name: $"redis-concurrent-access-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    var bucketId = (int)(scenarioContext.InvocationNumber % bucketCount);
                    var key = _context.GetBucketKey(bucketId);

                    // Use GetOrSet to test atomic operations with stampede protection
                    var factoryCalled = false;
                    var value = await _cacheProvider.GetOrSetAsync(
                        key,
                        async ct =>
                        {
                            factoryCalled = true;
                            // Simulate some work
                            await Task.Delay(10, ct).ConfigureAwait(false);
                            return $"value-{scenarioContext.InvocationNumber}";
                        },
                        _context.Options.DefaultExpiration,
                        CancellationToken.None).ConfigureAwait(false);

                    _metrics.AddOrUpdate(factoryCalled ? "factory_calls" : "cache_hits", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: factoryCalled ? "factory" : "cached");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                var factoryCalls = _metrics.GetValueOrDefault("factory_calls", 0);
                var cacheHits = _metrics.GetValueOrDefault("cache_hits", 0);
                Console.WriteLine($"Redis concurrent ({_context.ProviderName}) - Factory calls: {factoryCalls}, Cache hits: {cacheHits}");
                _metrics.Clear();
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
    /// Creates the expiration accuracy scenario.
    /// Tests TTL precision under high load.
    /// Target: &lt;100ms drift from expected expiration.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateExpirationAccuracyScenario()
    {
        var expirationTtl = TimeSpan.FromSeconds(5);

        return Scenario.Create(
            name: $"redis-expiration-accuracy-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    if (_redisFactory?.GetDatabase() is null)
                    {
                        return Response.Fail("Redis factory not available", statusCode: "no_provider");
                    }

                    var database = _redisFactory.GetDatabase()!;
                    var key = _context.NextCacheKeyWithPrefix("ttl");
                    var value = "ttl-test";

                    // Set with known TTL
                    var setTime = Stopwatch.GetTimestamp();
                    await _cacheProvider.SetAsync(key, value, expirationTtl, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Get actual TTL from Redis
                    var ttl = await database.KeyTimeToLiveAsync(key).ConfigureAwait(false);

                    if (ttl.HasValue)
                    {
                        var expectedTtlMs = expirationTtl.TotalMilliseconds;
                        var actualTtlMs = ttl.Value.TotalMilliseconds;
                        var driftMs = Math.Abs(expectedTtlMs - actualTtlMs);

                        _metrics.AddOrUpdate("total_drift_ms", (long)driftMs, (_, c) => c + (long)driftMs);
                        _metrics.AddOrUpdate("ttl_checks", 1, (_, c) => c + 1);

                        if (driftMs < 100)
                        {
                            return Response.Ok(statusCode: "accurate");
                        }
                        else if (driftMs < 500)
                        {
                            return Response.Ok(statusCode: "minor_drift");
                        }
                        else
                        {
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
            .WithClean(_ =>
            {
                var totalDrift = _metrics.GetValueOrDefault("total_drift_ms", 0);
                var checks = _metrics.GetValueOrDefault("ttl_checks", 0);
                var avgDrift = checks > 0 ? (double)totalDrift / checks : 0;
                Console.WriteLine($"Redis expiration ({_context.ProviderName}) - Average drift: {avgDrift:F2}ms over {checks} checks");
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
    /// Creates the pipeline batching scenario.
    /// Compares throughput of batched vs individual operations.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreatePipelineBatchingScenario()
    {
        const int batchSize = 10;

        return Scenario.Create(
            name: $"redis-pipeline-batching-{_context.ProviderName}",
            run: async scenarioContext =>
            {
                try
                {
                    if (_redisFactory?.GetDatabase() is null)
                    {
                        return Response.Fail("Provider not initialized", statusCode: "no_provider");
                    }

                    var database = _redisFactory.GetDatabase()!;
                    var usePipeline = scenarioContext.InvocationNumber % 2 == 0;

                    if (usePipeline)
                    {
                        // Batched pipeline operations
                        var batch = database.CreateBatch();
                        var tasks = new List<Task>();

                        for (var i = 0; i < batchSize; i++)
                        {
                            var key = _context.NextCacheKey();
                            var value = _context.CreateTestData();
                            tasks.Add(batch.StringSetAsync(key, value, _context.Options.DefaultExpiration));
                        }

                        batch.Execute();
                        await Task.WhenAll(tasks).ConfigureAwait(false);

                        _metrics.AddOrUpdate("pipeline_ops", batchSize, (_, c) => c + batchSize);
                        return Response.Ok(statusCode: "pipeline");
                    }
                    else
                    {
                        // Individual operations
                        for (var i = 0; i < batchSize; i++)
                        {
                            var key = _context.NextCacheKey();
                            var value = _context.CreateTestData();
                            await database.StringSetAsync(key, value, _context.Options.DefaultExpiration)
                                .ConfigureAwait(false);
                        }

                        _metrics.AddOrUpdate("individual_ops", batchSize, (_, c) => c + batchSize);
                        return Response.Ok(statusCode: "individual");
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                var pipelineOps = _metrics.GetValueOrDefault("pipeline_ops", 0);
                var individualOps = _metrics.GetValueOrDefault("individual_ops", 0);
                Console.WriteLine($"Redis pipeline ({_context.ProviderName}) - Pipeline ops: {pipelineOps}, Individual ops: {individualOps}");
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
