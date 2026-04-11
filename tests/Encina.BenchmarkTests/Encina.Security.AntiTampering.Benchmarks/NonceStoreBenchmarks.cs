using Encina.Security.AntiTampering.Nonce;

namespace Encina.Security.AntiTampering.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="InMemoryNonceStore"/> operations.
/// Measures throughput and memory allocation for nonce add, lookup, and cleanup operations.
/// </summary>
[MemoryDiagnoser]
// Palanca 2: iterationCount raised from 10 to 20 and warmupCount from 3 to 5 so
// Add (CoV 8.36 % at N=8, borderline on the two-tier stability rule) settles
// into the stable bucket with a larger sample.
[SimpleJob(warmupCount: 5, iterationCount: 20)]
public class NonceStoreBenchmarks : IDisposable
{
    private InMemoryNonceStore _store = null!;
    private string[] _preloadedNonces = null!;
    private static readonly TimeSpan NonceExpiry = TimeSpan.FromMinutes(10);

    [GlobalSetup]
    public void Setup()
    {
        _store = new InMemoryNonceStore(TimeProvider.System);

        // Pre-load 10,000 nonces for exists/duplicate benchmarks
        _preloadedNonces = new string[10_000];

        for (var i = 0; i < 10_000; i++)
        {
            _preloadedNonces[i] = $"preloaded-nonce-{i:D6}";
            _store.TryAddAsync(_preloadedNonces[i], NonceExpiry)
                .AsTask().GetAwaiter().GetResult();
        }
    }

    #region TryAddAsync Benchmarks

    [Benchmark(Baseline = true)]
    public async Task<bool> Add()
    {
        return await _store.TryAddAsync(Guid.NewGuid().ToString("N"), NonceExpiry);
    }

    #endregion

    #region ExistsAsync Benchmarks

    [Benchmark]
    public async Task<bool> Exists_Hit()
    {
        return await _store.ExistsAsync(_preloadedNonces[5000]);
    }

    [Benchmark]
    public async Task<bool> Exists_Miss()
    {
        return await _store.ExistsAsync("nonexistent-nonce-value");
    }

    #endregion

    #region Cleanup Benchmarks

    [Benchmark]
    public async Task<int> Cleanup()
    {
        // Create a store with expired entries to measure cleanup cost
        using var expiredStore = new InMemoryNonceStore(TimeProvider.System);
        var veryShortExpiry = TimeSpan.FromMilliseconds(1);

        for (var i = 0; i < 1_000; i++)
        {
            await expiredStore.TryAddAsync($"expired-{i:D4}", veryShortExpiry);
        }

        // Wait for entries to expire
        await Task.Delay(5);

        // Force cleanup by adding and checking a nonce (triggers lazy cleanup)
        var added = await expiredStore.TryAddAsync("cleanup-trigger", NonceExpiry);
        var exists = await expiredStore.ExistsAsync("cleanup-trigger");

        return added && exists ? 1 : 0;
    }

    #endregion

    public void Dispose()
    {
        _store?.Dispose();
    }
}
