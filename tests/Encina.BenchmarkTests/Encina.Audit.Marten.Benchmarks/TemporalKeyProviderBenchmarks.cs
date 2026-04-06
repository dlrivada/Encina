using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Audit.Marten.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="InMemoryTemporalKeyProvider"/> key lookup and creation throughput.
/// Measures the key management overhead per audit entry.
/// </summary>
/// <remarks>
/// <para>
/// Every <c>RecordAsync</c> call invokes <c>GetOrCreateKeyAsync</c> to obtain the temporal key
/// for the current period. This benchmark measures whether that lookup is a bottleneck.
/// </para>
/// <para>
/// Run:
/// <code>
/// dotnet run -c Release -- --filter "*TemporalKeyProvider*" --job short
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class TemporalKeyProviderBenchmarks
{
    private InMemoryTemporalKeyProvider _keyProvider = null!;
    private string[] _existingPeriods = null!;

    [Params(12, 84)]
    public int PeriodCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keyProvider = new InMemoryTemporalKeyProvider(
            TimeProvider.System,
            NullLogger<InMemoryTemporalKeyProvider>.Instance);

        _existingPeriods = new string[PeriodCount];
        for (var i = 0; i < PeriodCount; i++)
        {
            var year = 2020 + (i / 12);
            var month = (i % 12) + 1;
            var period = $"{year}-{month:D2}";
            _existingPeriods[i] = period;
            _keyProvider.GetOrCreateKeyAsync(period)
                .AsTask().GetAwaiter().GetResult();
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _keyProvider.Clear();
    }

    /// <summary>
    /// Hot path: retrieve existing key for current period (most common operation).
    /// </summary>
    [BenchmarkCategory("DocRef:bench:audit-marten/key-lookup-get-existing")]
    [Benchmark(Baseline = true)]
    public async Task<TemporalKeyInfo?> GetExistingKey()
    {
        var period = _existingPeriods[0];
        var result = await _keyProvider.GetKeyAsync(period);
        TemporalKeyInfo? info = null;
        result.IfRight(k => info = k);
        return info;
    }

    /// <summary>
    /// Hot path: GetOrCreate for existing period (idempotent, should be fast).
    /// </summary>
    [Benchmark]
    public async Task<TemporalKeyInfo?> GetOrCreateExistingKey()
    {
        var period = _existingPeriods[0];
        var result = await _keyProvider.GetOrCreateKeyAsync(period);
        TemporalKeyInfo? info = null;
        result.IfRight(k => info = k);
        return info;
    }

    /// <summary>
    /// Cold path: create a new key for a new period (happens once per period).
    /// </summary>
    [BenchmarkCategory("DocRef:bench:audit-marten/key-lookup-create-new")]
    [Benchmark]
    public async Task<TemporalKeyInfo?> CreateNewKey()
    {
        var period = $"new-{Guid.NewGuid():N}";
        var result = await _keyProvider.GetOrCreateKeyAsync(period);
        TemporalKeyInfo? info = null;
        result.IfRight(k => info = k);
        return info;
    }

    /// <summary>
    /// Check if a period has been destroyed (used in projections).
    /// </summary>
    [Benchmark]
    public async Task<bool> IsKeyDestroyed()
    {
        var period = _existingPeriods[0];
        var result = await _keyProvider.IsKeyDestroyedAsync(period);
        bool destroyed = false;
        result.IfRight(d => destroyed = d);
        return destroyed;
    }

    /// <summary>
    /// List all active keys (used in health checks).
    /// </summary>
    [Benchmark]
    public async Task<int> GetActiveKeysCount()
    {
        var result = await _keyProvider.GetActiveKeysAsync();
        int count = 0;
        result.IfRight(keys => count = keys.Count);
        return count;
    }
}
