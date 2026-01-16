using BenchmarkDotNet.Attributes;

namespace Encina.Polly.Benchmarks;

/// <summary>
/// Benchmarks for rate limiting functionality.
/// Measures overhead of rate limiting and state management.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class RateLimitingBenchmarks
{
    private AdaptiveRateLimiter _rateLimiter = null!;
    private RateLimitAttribute _highLimitAttribute = null!;
    private RateLimitAttribute _adaptiveAttribute = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rateLimiter = new AdaptiveRateLimiter();

        // High limit to avoid hitting the limit during benchmarks
        _highLimitAttribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100000,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = false
        };

        // With adaptive throttling enabled
        _adaptiveAttribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100000,
            WindowSizeSeconds = 60,
            EnableAdaptiveThrottling = true,
            ErrorThresholdPercent = 50,
            MinimumThroughputForThrottling = 10
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Reset the rate limiter before each iteration to ensure consistent state
        _rateLimiter.Reset("benchmark-key");
        _rateLimiter.Reset("adaptive-key");
    }

    [Benchmark(Baseline = true)]
    public async Task<RateLimitResult> AcquireAsync_SimpleRateLimiting()
    {
        return await _rateLimiter.AcquireAsync("benchmark-key", _highLimitAttribute, CancellationToken.None);
    }

    [Benchmark]
    public async Task<RateLimitResult> AcquireAsync_WithAdaptiveThrottling()
    {
        return await _rateLimiter.AcquireAsync("adaptive-key", _adaptiveAttribute, CancellationToken.None);
    }

    [Benchmark]
    public void RecordSuccess()
    {
        _rateLimiter.RecordSuccess("benchmark-key");
    }

    [Benchmark]
    public void RecordFailure()
    {
        _rateLimiter.RecordFailure("benchmark-key");
    }

    [Benchmark]
    public RateLimitState? GetState()
    {
        return _rateLimiter.GetState("benchmark-key");
    }

    [Benchmark]
    public async Task AcquireAndRecordSuccess_Combined()
    {
        await _rateLimiter.AcquireAsync("benchmark-key", _highLimitAttribute, CancellationToken.None);
        _rateLimiter.RecordSuccess("benchmark-key");
    }

    [Benchmark]
    public async Task AcquireAndRecordFailure_Combined()
    {
        await _rateLimiter.AcquireAsync("benchmark-key", _highLimitAttribute, CancellationToken.None);
        _rateLimiter.RecordFailure("benchmark-key");
    }
}

/// <summary>
/// Benchmarks comparing multiple keys performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class RateLimitingMultiKeyBenchmarks
{
    private AdaptiveRateLimiter _rateLimiter = null!;
    private RateLimitAttribute _attribute = null!;
    private string[] _keys = null!;

    [Params(1, 10, 100)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rateLimiter = new AdaptiveRateLimiter();
        _attribute = new RateLimitAttribute
        {
            MaxRequestsPerWindow = 100000,
            WindowSizeSeconds = 60
        };
        _keys = Enumerable.Range(0, KeyCount).Select(i => $"key-{i}").ToArray();
    }

    [Benchmark]
    public async Task AcquireAcrossMultipleKeys()
    {
        foreach (var key in _keys)
        {
            await _rateLimiter.AcquireAsync(key, _attribute, CancellationToken.None);
        }
    }
}
