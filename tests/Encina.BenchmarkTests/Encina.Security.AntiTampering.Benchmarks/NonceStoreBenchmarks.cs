using Encina.Security.AntiTampering.Nonce;

namespace Encina.Security.AntiTampering.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="InMemoryNonceStore"/> operations.
/// Measures throughput and memory allocation for nonce add and lookup operations
/// under varying store sizes and concurrency levels.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class NonceStoreBenchmarks : IDisposable
{
    private InMemoryNonceStore _emptyStore = null!;
    private InMemoryNonceStore _preloadedStore = null!;
    private string[] _preloadedNonces = null!;
    private static readonly TimeSpan NonceExpiry = TimeSpan.FromMinutes(10);

    [GlobalSetup]
    public void Setup()
    {
        _emptyStore = new InMemoryNonceStore(TimeProvider.System);
        _preloadedStore = new InMemoryNonceStore(TimeProvider.System);

        // Pre-load 10,000 nonces
        _preloadedNonces = new string[10_000];

        for (var i = 0; i < 10_000; i++)
        {
            _preloadedNonces[i] = $"preloaded-nonce-{i:D6}";
            _preloadedStore.TryAddAsync(_preloadedNonces[i], NonceExpiry)
                .AsTask().GetAwaiter().GetResult();
        }
    }

    [IterationSetup(Target = nameof(TryAdd_EmptyStore))]
    public void ResetEmptyStore()
    {
        _emptyStore.Dispose();
        _emptyStore = new InMemoryNonceStore(TimeProvider.System);
    }

    #region TryAddAsync Benchmarks

    [Benchmark(Baseline = true)]
    public async Task<bool> TryAdd_EmptyStore()
    {
        return await _emptyStore.TryAddAsync(Guid.NewGuid().ToString("N"), NonceExpiry);
    }

    [Benchmark]
    public async Task<bool> TryAdd_PreloadedStore_10K()
    {
        // Add a new unique nonce to a store already containing 10K entries
        return await _preloadedStore.TryAddAsync(Guid.NewGuid().ToString("N"), NonceExpiry);
    }

    [Benchmark]
    public async Task<bool> TryAdd_DuplicateNonce()
    {
        // Attempt to add an existing nonce (should return false)
        return await _preloadedStore.TryAddAsync(_preloadedNonces[0], NonceExpiry);
    }

    #endregion

    #region ExistsAsync Benchmarks

    [Benchmark]
    public async Task<bool> Exists_KnownNonce()
    {
        return await _preloadedStore.ExistsAsync(_preloadedNonces[5000]);
    }

    [Benchmark]
    public async Task<bool> Exists_UnknownNonce()
    {
        return await _preloadedStore.ExistsAsync("nonexistent-nonce");
    }

    #endregion

    #region Concurrent Benchmarks

    [Benchmark]
    public async Task<int> Concurrent_TryAdd_10Threads()
    {
        using var store = new InMemoryNonceStore(TimeProvider.System);
        var successCount = 0;

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var nonce = $"concurrent-{i}-{Guid.NewGuid():N}";
            var added = await store.TryAddAsync(nonce, NonceExpiry);

            if (added)
            {
                Interlocked.Increment(ref successCount);
            }
        });

        await Task.WhenAll(tasks);
        return successCount;
    }

    [Benchmark]
    public async Task<int> Concurrent_DuplicateNonce_10Threads()
    {
        using var store = new InMemoryNonceStore(TimeProvider.System);
        var sharedNonce = Guid.NewGuid().ToString("N");
        var successCount = 0;

        var tasks = Enumerable.Range(0, 10).Select(async _ =>
        {
            var added = await store.TryAddAsync(sharedNonce, NonceExpiry);

            if (added)
            {
                Interlocked.Increment(ref successCount);
            }
        });

        await Task.WhenAll(tasks);
        return successCount; // Should be exactly 1
    }

    #endregion

    public void Dispose()
    {
        _emptyStore?.Dispose();
        _preloadedStore?.Dispose();
    }
}
