namespace Encina.NBomber.Scenarios.Locking;

/// <summary>
/// Context object shared across locking load test scenarios.
/// Provides thread-safe resource ID generation and test data helpers.
/// </summary>
/// <param name="ProviderFactory">The lock provider factory.</param>
/// <param name="ProviderName">The name of the lock provider being tested.</param>
public sealed record LockScenarioContext(
    ILockProviderFactory ProviderFactory,
    string ProviderName)
{
    private long _resourceSequence;

    /// <summary>
    /// Gets the provider options.
    /// </summary>
    public LockProviderOptions Options => ProviderFactory.Options;

    /// <summary>
    /// Generates a unique resource ID for lock acquisition.
    /// Thread-safe.
    /// </summary>
    /// <returns>A unique resource ID.</returns>
    public string NextResourceId()
    {
        var sequence = Interlocked.Increment(ref _resourceSequence);
        return $"{Options.KeyPrefix}:resource:{sequence}";
    }

    /// <summary>
    /// Generates a resource ID for a specific bucket (shared resource for contention testing).
    /// </summary>
    /// <param name="bucketId">The bucket identifier (0-based).</param>
    /// <returns>A resource ID for the specified bucket.</returns>
    public string GetBucketResource(int bucketId)
    {
        return $"{Options.KeyPrefix}:bucket:{bucketId}";
    }

    /// <summary>
    /// Gets a random bucket ID for contention testing.
    /// </summary>
    /// <param name="invocationNumber">The scenario invocation number.</param>
    /// <returns>A bucket ID.</returns>
    public int GetRandomBucket(long invocationNumber)
    {
        return (int)(invocationNumber % Options.ContentionBuckets);
    }

    /// <summary>
    /// Gets the current sequence number (for metrics/logging).
    /// </summary>
    public long CurrentSequence => Interlocked.Read(ref _resourceSequence);
}
