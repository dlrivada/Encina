using System.Collections.Concurrent;
using Encina.Caching;
using Encina.NBomber.Scenarios.Caching.Providers;
using NBomber.Contracts;
using NBomber.CSharp;

namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Factory for creating hybrid cache load test scenarios.
/// Tests L1/L2 synchronization, stampede prevention, and invalidation propagation.
/// </summary>
public sealed class HybridCacheScenarioFactory
{
    private readonly CacheScenarioContext _context;
    private readonly ICacheProvider _cacheProvider;
    private readonly HybridCacheProviderFactory? _hybridFactory;
    private readonly ConcurrentDictionary<string, long> _metrics = new();
    private bool _cacheInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="HybridCacheScenarioFactory"/> class.
    /// </summary>
    /// <param name="context">The cache scenario context.</param>
    public HybridCacheScenarioFactory(CacheScenarioContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _hybridFactory = context.ProviderFactory as HybridCacheProviderFactory;
        // Create cache provider once - shared across all scenarios
        _cacheProvider = _context.ProviderFactory.CreateCacheProvider();
    }

    /// <summary>
    /// Creates all hybrid cache scenarios.
    /// </summary>
    /// <returns>A collection of load test scenarios.</returns>
    public IEnumerable<ScenarioProps> CreateScenarios()
    {
        yield return CreateL1L2SyncScenario();
        yield return CreateStampedePreventionScenario();
        yield return CreateInvalidationPropagationScenario();
    }

    /// <summary>
    /// Creates the L1/L2 synchronization scenario.
    /// Verifies memory (L1) and Redis (L2) stay synchronized under concurrent access.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateL1L2SyncScenario()
    {
        const int keyCount = 100;

        return Scenario.Create(
            name: "hybrid-l1-l2-sync",
            run: async scenarioContext =>
            {
                try
                {
                    // Use limited key set to force L1 hits
                    var keyIndex = (int)(scenarioContext.InvocationNumber % keyCount);
                    var key = $"{_context.Options.KeyPrefix}:sync:{keyIndex}";

                    var operation = scenarioContext.InvocationNumber % 5;

                    switch (operation)
                    {
                        case 0: // Write new value
                            var newValue = $"value-{scenarioContext.InvocationNumber}";
                            await _cacheProvider.SetAsync(key, newValue, _context.Options.DefaultExpiration, CancellationToken.None)
                                .ConfigureAwait(false);
                            _metrics.AddOrUpdate("writes", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "write");

                        case 1 or 2 or 3: // Read (more frequent to test L1 hits)
                            var readValue = await _cacheProvider.GetAsync<string>(key, CancellationToken.None)
                                .ConfigureAwait(false);

                            if (readValue is not null)
                            {
                                _metrics.AddOrUpdate("hits", 1, (_, c) => c + 1);
                                return Response.Ok(statusCode: "hit");
                            }

                            _metrics.AddOrUpdate("misses", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "miss");

                        default: // Remove to test invalidation
                            await _cacheProvider.RemoveAsync(key, CancellationToken.None)
                                .ConfigureAwait(false);
                            _metrics.AddOrUpdate("removes", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "remove");
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(async _ =>
            {
                // Initialize only once
                if (!_cacheInitialized)
                {
                    if (_hybridFactory is not null)
                    {
                        await _hybridFactory.FlushL2CacheAsync().ConfigureAwait(false);
                    }

                    // Pre-populate some keys
                    for (var i = 0; i < 50; i++)
                    {
                        var key = $"{_context.Options.KeyPrefix}:sync:{i}";
                        await _cacheProvider.SetAsync(key, $"initial-{i}", _context.Options.DefaultExpiration, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    _cacheInitialized = true;
                }
            })
            .WithClean(_ =>
            {
                Console.WriteLine($"Hybrid L1/L2 sync - Writes: {_metrics.GetValueOrDefault("writes", 0)}, " +
                    $"Hits: {_metrics.GetValueOrDefault("hits", 0)}, " +
                    $"Misses: {_metrics.GetValueOrDefault("misses", 0)}, " +
                    $"Removes: {_metrics.GetValueOrDefault("removes", 0)}");
                _metrics.Clear();
                return Task.CompletedTask;
            })
            .WithoutWarmUp()
            .WithLoadSimulations(
                Simulation.Inject(
                    rate: 150,
                    interval: TimeSpan.FromSeconds(1),
                    during: TimeSpan.FromMinutes(1)));
    }

    /// <summary>
    /// Creates the stampede prevention scenario.
    /// Tests that concurrent requests for the same uncached key share a single backend fetch.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateStampedePreventionScenario()
    {
        const int bucketCount = 5;
        var factoryCallCounts = new ConcurrentDictionary<string, int>();

        return Scenario.Create(
            name: "hybrid-stampede-prevention",
            run: async scenarioContext =>
            {
                try
                {
                    // Force multiple concurrent requests for the same key
                    var bucketId = (int)((scenarioContext.InvocationNumber / 10) % bucketCount);
                    var key = $"{_context.Options.KeyPrefix}:stampede:{bucketId}";

                    var value = await _cacheProvider.GetOrSetAsync(
                        key,
                        async ct =>
                        {
                            factoryCallCounts.AddOrUpdate(key, 1, (_, c) => c + 1);
                            _metrics.AddOrUpdate("factory_calls", 1, (_, c) => c + 1);

                            // Simulate expensive operation
                            await Task.Delay(50, ct).ConfigureAwait(false);
                            return $"expensive-value-{key}";
                        },
                        _context.Options.DefaultExpiration,
                        CancellationToken.None).ConfigureAwait(false);

                    _metrics.AddOrUpdate("total_requests", 1, (_, c) => c + 1);
                    return Response.Ok(statusCode: "completed");
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithInit(_ =>
            {
                factoryCallCounts.Clear();
                return Task.CompletedTask;
            })
            .WithClean(_ =>
            {
                var totalRequests = _metrics.GetValueOrDefault("total_requests", 0);
                var factoryCalls = _metrics.GetValueOrDefault("factory_calls", 0);
                var stampedePrevention = totalRequests > 0 ? (double)(totalRequests - factoryCalls) / totalRequests * 100 : 0;

                Console.WriteLine($"Hybrid stampede prevention - Total requests: {totalRequests}, " +
                    $"Factory calls: {factoryCalls}, " +
                    $"Stampede prevention: {stampedePrevention:F1}%");

                // Log per-key factory calls
                foreach (var kvp in factoryCallCounts.OrderBy(k => k.Key))
                {
                    Console.WriteLine($"  {kvp.Key}: {kvp.Value} factory calls");
                }

                _metrics.Clear();
                factoryCallCounts.Clear();
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
    /// Creates the invalidation propagation scenario.
    /// Measures cache invalidation propagation across L1/L2 layers.
    /// </summary>
    /// <returns>The scenario configuration.</returns>
    public ScenarioProps CreateInvalidationPropagationScenario()
    {
        const int keyCount = 50;

        return Scenario.Create(
            name: "hybrid-invalidation-propagation",
            run: async scenarioContext =>
            {
                try
                {
                    var keyIndex = (int)(scenarioContext.InvocationNumber % keyCount);
                    var key = $"{_context.Options.KeyPrefix}:invalidate:{keyIndex}";

                    // Pattern: Write -> Read (verify) -> Invalidate -> Read (verify gone)
                    var phase = scenarioContext.InvocationNumber % 4;

                    switch (phase)
                    {
                        case 0: // Write
                            var writeValue = $"value-{scenarioContext.InvocationNumber}";
                            await _cacheProvider.SetAsync(key, writeValue, _context.Options.DefaultExpiration, CancellationToken.None)
                                .ConfigureAwait(false);
                            _metrics.AddOrUpdate("writes", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "write");

                        case 1: // Read (should hit)
                            var readValue = await _cacheProvider.GetAsync<string>(key, CancellationToken.None)
                                .ConfigureAwait(false);

                            if (readValue is not null)
                            {
                                _metrics.AddOrUpdate("pre_invalidate_hits", 1, (_, c) => c + 1);
                                return Response.Ok(statusCode: "pre_hit");
                            }

                            _metrics.AddOrUpdate("pre_invalidate_misses", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "pre_miss");

                        case 2: // Invalidate
                            await _cacheProvider.RemoveAsync(key, CancellationToken.None)
                                .ConfigureAwait(false);
                            _metrics.AddOrUpdate("invalidates", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "invalidate");

                        default: // Read after invalidation (should miss)
                            var postValue = await _cacheProvider.GetAsync<string>(key, CancellationToken.None)
                                .ConfigureAwait(false);

                            if (postValue is null)
                            {
                                _metrics.AddOrUpdate("post_invalidate_misses", 1, (_, c) => c + 1);
                                return Response.Ok(statusCode: "post_miss");
                            }

                            // Unexpected hit after invalidation - possible propagation delay
                            _metrics.AddOrUpdate("propagation_delays", 1, (_, c) => c + 1);
                            return Response.Ok(statusCode: "propagation_delay");
                    }
                }
                catch (Exception ex)
                {
                    return Response.Fail(ex.Message, statusCode: "exception");
                }
            })
            .WithClean(_ =>
            {
                var writes = _metrics.GetValueOrDefault("writes", 0);
                var preHits = _metrics.GetValueOrDefault("pre_invalidate_hits", 0);
                var preMisses = _metrics.GetValueOrDefault("pre_invalidate_misses", 0);
                var invalidates = _metrics.GetValueOrDefault("invalidates", 0);
                var postMisses = _metrics.GetValueOrDefault("post_invalidate_misses", 0);
                var delays = _metrics.GetValueOrDefault("propagation_delays", 0);

                var postTotal = postMisses + delays;
                var propagationAccuracy = postTotal > 0 ? (double)postMisses / postTotal * 100 : 100;

                Console.WriteLine($"Hybrid invalidation - Writes: {writes}, " +
                    $"Pre-invalidate hits: {preHits}, " +
                    $"Invalidates: {invalidates}, " +
                    $"Post-invalidate misses: {postMisses}, " +
                    $"Propagation delays: {delays}, " +
                    $"Propagation accuracy: {propagationAccuracy:F1}%");

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
}
