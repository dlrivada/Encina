using BenchmarkDotNet.Attributes;

namespace Encina.Polly.Benchmarks;

/// <summary>
/// Benchmarks for bulkhead isolation functionality.
/// Measures overhead of bulkhead acquire/release operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class BulkheadBenchmarks : IDisposable
{
    private BulkheadManager _bulkheadManager = null!;
    private BulkheadAttribute _highLimitAttribute = null!;
    private BulkheadAttribute _smallLimitAttribute = null!;

    [GlobalSetup]
    public void Setup()
    {
        _bulkheadManager = new BulkheadManager();

        // High limit to avoid hitting the limit during benchmarks
        _highLimitAttribute = new BulkheadAttribute
        {
            MaxConcurrency = 10000,
            MaxQueuedActions = 10000,
            QueueTimeoutMs = 30000
        };

        // Small limit for queue testing
        _smallLimitAttribute = new BulkheadAttribute
        {
            MaxConcurrency = 10,
            MaxQueuedActions = 100,
            QueueTimeoutMs = 30000
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Reset the bulkhead before each iteration
        _bulkheadManager.Reset("benchmark-key");
        _bulkheadManager.Reset("small-key");
    }

    public void Dispose()
    {
        _bulkheadManager?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    public async Task<BulkheadAcquireResult> TryAcquireAsync_HighLimit()
    {
        var result = await _bulkheadManager.TryAcquireAsync("benchmark-key", _highLimitAttribute);
        result.Releaser?.Dispose();
        return result;
    }

    [Benchmark]
    public async Task<BulkheadAcquireResult> TryAcquireAsync_SmallLimit()
    {
        var result = await _bulkheadManager.TryAcquireAsync("small-key", _smallLimitAttribute);
        result.Releaser?.Dispose();
        return result;
    }

    [Benchmark]
    public BulkheadMetrics? GetMetrics()
    {
        return _bulkheadManager.GetMetrics("benchmark-key");
    }

    [Benchmark]
    public async Task AcquireAndRelease_Cycle()
    {
        var result = await _bulkheadManager.TryAcquireAsync("benchmark-key", _highLimitAttribute);
        result.Releaser?.Dispose();
    }

    [Benchmark]
    public async Task AcquireMultiple_ThenReleaseAll()
    {
        var permits = new List<BulkheadAcquireResult>(10);

        for (int i = 0; i < 10; i++)
        {
            permits.Add(await _bulkheadManager.TryAcquireAsync("benchmark-key", _highLimitAttribute));
        }

        foreach (var permit in permits)
        {
            permit.Releaser?.Dispose();
        }
    }
}

/// <summary>
/// Benchmarks comparing multiple keys performance for bulkhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class BulkheadMultiKeyBenchmarks : IDisposable
{
    private BulkheadManager _bulkheadManager = null!;
    private BulkheadAttribute _attribute = null!;
    private string[] _keys = null!;

    [Params(1, 10, 100)]
    public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bulkheadManager = new BulkheadManager();
        _attribute = new BulkheadAttribute
        {
            MaxConcurrency = 1000,
            MaxQueuedActions = 1000
        };
        _keys = Enumerable.Range(0, KeyCount).Select(i => $"key-{i}").ToArray();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        foreach (var key in _keys)
        {
            _bulkheadManager.Reset(key);
        }
    }

    public void Dispose()
    {
        _bulkheadManager?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task AcquireAcrossMultipleKeys()
    {
        foreach (var key in _keys)
        {
            var result = await _bulkheadManager.TryAcquireAsync(key, _attribute);
            result.Releaser?.Dispose();
        }
    }
}

/// <summary>
/// Benchmarks for concurrent bulkhead operations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 2, iterationCount: 3)]
public class BulkheadConcurrencyBenchmarks : IDisposable
{
    private BulkheadManager _bulkheadManager = null!;
    private BulkheadAttribute _attribute = null!;

    [Params(10, 50, 100)]
    public int ConcurrentRequests { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bulkheadManager = new BulkheadManager();
        _attribute = new BulkheadAttribute
        {
            MaxConcurrency = 1000,
            MaxQueuedActions = 1000,
            QueueTimeoutMs = 30000
        };
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _bulkheadManager.Reset("concurrent-key");
    }

    public void Dispose()
    {
        _bulkheadManager?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    public async Task ConcurrentAcquireAndRelease()
    {
        var tasks = Enumerable.Range(0, ConcurrentRequests).Select(async _ =>
        {
            var result = await _bulkheadManager.TryAcquireAsync("concurrent-key", _attribute);
            await Task.Yield(); // Simulate some work
            result.Releaser?.Dispose();
        });

        await Task.WhenAll(tasks);
    }
}
