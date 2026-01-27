using System.Collections.Concurrent;
using Encina.Caching;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Factory for creating memory cache load test scenarios.
/// Tests throughput, concurrent access, and eviction behavior.
/// </summary>
public sealed class MemoryCacheScenarioFactory
{
    private readonly CacheScenarioContext _context;
    private readonly ICacheProvider _cacheProvider;
    private readonly ConcurrentDictionary<string, long> _hitCounts = new();
    private readonly ConcurrentDictionary<string, long> _missCounts = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCacheScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The cache scenario context.</param>
    public MemoryCacheScenarioFactory(CacheScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        // Create cache provider once - shared across all scenarios
        _cacheProvider = _context.ProviderFactory.CreateCacheProvider();
    }

    /// <summary>
    /// Creates all memory cache scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateGetSetThroughputScenario();
        yield return CreateConcurrentAccessScenario();
        yield return CreateEvictionPressureScenario();
    }

    /// <summary>
    /// Creates the get/set throughput scenario.
    /// Tests high-rate read/write operations with varying value sizes.
    /// Target: 10,000+ ops/sec.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateGetSetThroughputScenario()
    {
        return Scenario.Create(
            name: $"memory-get-set-throughput",
            run: async scenarioContext =>
            {
                try
                {
                    var key = _context.NextCacheKey();
                    var isWrite = scenarioContext.InvocationNumber % 3 == 0; // 33% writes, 67% reads

                    if (isWrite)
                    {
                        // Vary value sizes: 100B, 1KB, 10KB
                        var sizeIndex = (int)(scenarioContext.InvocationNumber % 3);
                        var size = sizeIndex switch
                        {
                            0 => 100,
                            1 => 1024,
                            _ => 10240
                        };
                        var value = CacheScenarioContext.CreateTestData(size);

                        await _cacheProvider.SetAsync(key, value, _context.Options.DefaultExpiration, CancellationToken.None)
                            .ConfigureAwait(false);

                        return Response.Ok(statusCode: $"write:{size}B");
                    }
                    else
                    {
                        // Read from a previously written key or a random key
                        var readKey = scenarioContext.InvocationNumber > 10
                            ? $"{_context.Options.KeyPrefix}:key:{scenarioContext.InvocationNumber - 5}"
                            : key;

                        var value = await _cacheProvider.GetAsync<string>(readKey, CancellationToken.None)
                            .ConfigureAwait(false);

                        if (value is not null)
                        {
                            _hitCounts.AddOrUpdate("hits", 1, (_, count) => count + 1);
                            return Response.Ok(statusCode: "read:hit");
                        }

                        _missCounts.AddOrUpdate("misses", 1, (_, count) => count + 1);
                        return Response.Ok(statusCode: "read:miss");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Memory cache throughput exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                Console.WriteLine($"Memory cache throughput - Hits: {_hitCounts.GetValueOrDefault("hits", 0)}, Misses: {_missCounts.GetValueOrDefault("misses", 0)}");
                _hitCounts.Clear();
                _missCounts.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 500,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the concurrent access scenario.
    /// Tests concurrent operations on the same keys to verify thread-safety.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateConcurrentAccessScenario()
    {
        const int bucketCount = 10;

        return Scenario.Create(
            name: $"memory-concurrent-access",
            run: async scenarioContext =>
            {
                try
                {
                    // Use a limited set of keys to force concurrent access
                    var bucketId = (int)(scenarioContext.InvocationNumber % bucketCount);
                    var key = _context.GetBucketKey(bucketId);

                    // Mix of operations on the same key
                    var operation = scenarioContext.InvocationNumber % 4;

                    switch (operation)
                    {
                        case 0: // Write
                            var value = $"value-{scenarioContext.InvocationNumber}";
                            await _cacheProvider.SetAsync(key, value, _context.Options.DefaultExpiration, CancellationToken.None)
                                .ConfigureAwait(false);
                            return Response.Ok(statusCode: "write");

                        case 1: // Read
                            var readValue = await _cacheProvider.GetAsync<string>(key, CancellationToken.None)
                                .ConfigureAwait(false);
                            return Response.Ok(statusCode: readValue is not null ? "read:hit" : "read:miss");

                        case 2: // GetOrSet
                            var getOrSetValue = await _cacheProvider.GetOrSetAsync(
                                key,
                                _ => Task.FromResult($"factory-{scenarioContext.InvocationNumber}"),
                                _context.Options.DefaultExpiration,
                                CancellationToken.None).ConfigureAwait(false);
                            return Response.Ok(statusCode: "get_or_set");

                        default: // Exists
                            var exists = await _cacheProvider.ExistsAsync(key, CancellationToken.None)
                                .ConfigureAwait(false);
                            return Response.Ok(statusCode: exists ? "exists:yes" : "exists:no");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Memory concurrent access exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 200,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the eviction pressure scenario.
    /// Tests behavior when exceeding cache size limits.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateEvictionPressureScenario()
    {
        return Scenario.Create(
            name: $"memory-eviction-pressure",
            run: async scenarioContext =>
            {
                try
                {
                    // Create large values to trigger eviction faster
                    var key = _context.NextCacheKeyWithPrefix("eviction");
                    var largeValue = CacheScenarioContext.CreateTestData(10240); // 10KB values

                    await _cacheProvider.SetAsync(key, largeValue, _context.Options.DefaultExpiration, CancellationToken.None)
                        .ConfigureAwait(false);

                    // Try to read an earlier key (may have been evicted)
                    var earlierSequence = scenarioContext.InvocationNumber > 100
                        ? scenarioContext.InvocationNumber - 100
                        : 1;
                    var earlierKey = $"{_context.Options.KeyPrefix}:eviction:{earlierSequence}";

                    var retrievedValue = await _cacheProvider.GetAsync<string>(earlierKey, CancellationToken.None)
                        .ConfigureAwait(false);

                    if (retrievedValue is not null)
                    {
                        _hitCounts.AddOrUpdate("eviction_hits", 1, (_, count) => count + 1);
                        return Response.Ok(statusCode: "retained");
                    }

                    _missCounts.AddOrUpdate("eviction_misses", 1, (_, count) => count + 1);
                    return Response.Ok(statusCode: "evicted");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Memory eviction pressure exception: {ex}");
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                var hits = _hitCounts.GetValueOrDefault("eviction_hits", 0);
                var misses = _missCounts.GetValueOrDefault("eviction_misses", 0);
                var total = hits + misses;
                var evictionRate = total > 0 ? (double)misses / total * 100 : 0;
                Console.WriteLine($"Eviction pressure - Retained: {hits}, Evicted: {misses}, Eviction Rate: {evictionRate:F1}%");
                _hitCounts.Clear();
                _missCounts.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 100,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }
}
