namespace Encina.NBomber.Scenarios.Caching;

/// <summary>
/// Context object shared across caching load test scenarios.
/// Provides thread-safe key generation and test data helpers.
/// </summary>
/// <param name="ProviderFactory">The cache provider factory.</param>
/// <param name="ProviderName">The name of the cache provider being tested.</param>
public sealed record CacheScenarioContext(
    ICacheProviderFactory ProviderFactory,
    string ProviderName)
{
    private long _keySequence;
    private static readonly Random _random = new();

    /// <summary>
    /// Gets the provider options.
    /// </summary>
    public CacheProviderOptions Options => ProviderFactory.Options;

    /// <summary>
    /// Generates a unique cache key for load testing.
    /// Thread-safe.
    /// </summary>
    /// <returns>A unique cache key.</returns>
    public string NextCacheKey()
    {
        var sequence = Interlocked.Increment(ref _keySequence);
        return $"{Options.KeyPrefix}:key:{sequence}";
    }

    /// <summary>
    /// Generates a cache key with a specific prefix for testing patterns.
    /// </summary>
    /// <param name="prefix">The key prefix.</param>
    /// <returns>A unique cache key with the specified prefix.</returns>
    public string NextCacheKeyWithPrefix(string prefix)
    {
        var sequence = Interlocked.Increment(ref _keySequence);
        return $"{Options.KeyPrefix}:{prefix}:{sequence}";
    }

    /// <summary>
    /// Generates a deterministic cache key for same-key concurrent access testing.
    /// </summary>
    /// <param name="bucketId">The bucket identifier (0-based).</param>
    /// <returns>A cache key for the specified bucket.</returns>
    public string GetBucketKey(int bucketId)
    {
        return $"{Options.KeyPrefix}:bucket:{bucketId}";
    }

    /// <summary>
    /// Creates test data of the configured size.
    /// </summary>
    /// <returns>Test data string.</returns>
    public string CreateTestData()
    {
        return CreateTestData(Options.ValueSizeBytes);
    }

    /// <summary>
    /// Creates test data of the specified size.
    /// </summary>
    /// <param name="sizeBytes">The approximate size in bytes.</param>
    /// <returns>Test data string.</returns>
    public static string CreateTestData(int sizeBytes)
    {
        // Create a string that's approximately the requested size
        var chars = new char[sizeBytes];
        for (var i = 0; i < sizeBytes; i++)
        {
            chars[i] = (char)('A' + (i % 26));
        }

        return new string(chars);
    }

    /// <summary>
    /// Creates test data with random content.
    /// </summary>
    /// <param name="sizeBytes">The size in bytes.</param>
    /// <returns>Random test data string.</returns>
    public static string CreateRandomTestData(int sizeBytes)
    {
        var chars = new char[sizeBytes];
        lock (_random)
        {
            for (var i = 0; i < sizeBytes; i++)
            {
                chars[i] = (char)_random.Next('A', 'Z' + 1);
            }
        }

        return new string(chars);
    }

    /// <summary>
    /// Gets the current sequence number (for metrics/logging).
    /// </summary>
    public long CurrentSequence => Interlocked.Read(ref _keySequence);
}
