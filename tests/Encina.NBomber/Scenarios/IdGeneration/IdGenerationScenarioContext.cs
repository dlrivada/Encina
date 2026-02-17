using System.Collections.Concurrent;
using System.Globalization;

namespace Encina.NBomber.Scenarios.IdGeneration;

/// <summary>
/// Shared context for ID generation load testing scenarios.
/// Tracks generated IDs for collision detection across concurrent workers.
/// </summary>
public sealed class IdGenerationScenarioContext
{
    /// <summary>
    /// Thread-safe set for detecting Snowflake ID collisions.
    /// </summary>
    public ConcurrentDictionary<long, byte> SnowflakeIds { get; } = new();

    /// <summary>
    /// Thread-safe set for detecting ULID collisions.
    /// </summary>
    public ConcurrentDictionary<string, byte> UlidIds { get; } = new();

    /// <summary>
    /// Thread-safe set for detecting UUIDv7 collisions.
    /// </summary>
    public ConcurrentDictionary<Guid, byte> UuidV7Ids { get; } = new();

    /// <summary>
    /// Thread-safe set for detecting ShardPrefixed ID collisions.
    /// </summary>
    public ConcurrentDictionary<string, byte> ShardPrefixedIds { get; } = new();

    /// <summary>
    /// Thread-safe collision counter for Snowflake IDs.
    /// </summary>
    private long _snowflakeCollisions;

    /// <summary>
    /// Thread-safe collision counter for ULID IDs.
    /// </summary>
    private long _ulidCollisions;

    /// <summary>
    /// Thread-safe collision counter for UUIDv7 IDs.
    /// </summary>
    private long _uuidV7Collisions;

    /// <summary>
    /// Thread-safe collision counter for ShardPrefixed IDs.
    /// </summary>
    private long _shardPrefixedCollisions;

    /// <summary>
    /// Records a Snowflake ID collision.
    /// </summary>
    public void RecordSnowflakeCollision() => Interlocked.Increment(ref _snowflakeCollisions);

    /// <summary>
    /// Records a ULID collision.
    /// </summary>
    public void RecordUlidCollision() => Interlocked.Increment(ref _ulidCollisions);

    /// <summary>
    /// Records a UUIDv7 collision.
    /// </summary>
    public void RecordUuidV7Collision() => Interlocked.Increment(ref _uuidV7Collisions);

    /// <summary>
    /// Records a ShardPrefixed ID collision.
    /// </summary>
    public void RecordShardPrefixedCollision() => Interlocked.Increment(ref _shardPrefixedCollisions);

    /// <summary>
    /// Gets the total number of Snowflake ID collisions detected.
    /// </summary>
    public long SnowflakeCollisions => Interlocked.Read(ref _snowflakeCollisions);

    /// <summary>
    /// Gets the total number of ULID collisions detected.
    /// </summary>
    public long UlidCollisions => Interlocked.Read(ref _ulidCollisions);

    /// <summary>
    /// Gets the total number of UUIDv7 collisions detected.
    /// </summary>
    public long UuidV7Collisions => Interlocked.Read(ref _uuidV7Collisions);

    /// <summary>
    /// Gets the total number of ShardPrefixed ID collisions detected.
    /// </summary>
    public long ShardPrefixedCollisions => Interlocked.Read(ref _shardPrefixedCollisions);

    /// <summary>
    /// Thread-safe entity counter for shard ID rotation.
    /// </summary>
    private long _shardSequence;

    /// <summary>
    /// Gets the next shard ID string, rotating through a configurable number of shards.
    /// </summary>
    /// <param name="shardCount">Total number of shards to rotate through.</param>
    /// <returns>A shard ID string like "shard-01".</returns>
    public string GetNextShardId(int shardCount = 10)
    {
        var index = Interlocked.Increment(ref _shardSequence) % shardCount;
        return $"shard-{index:D2}";
    }

    /// <summary>
    /// Gets the next numeric shard ID for Snowflake generators.
    /// </summary>
    /// <param name="maxShardId">Maximum shard ID value (inclusive).</param>
    /// <returns>A numeric shard ID string.</returns>
    public string GetNextNumericShardId(long maxShardId = 1023)
    {
        var index = Interlocked.Increment(ref _shardSequence) % (maxShardId + 1);
        return index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Prints a collision summary to the console.
    /// </summary>
    public void PrintCollisionSummary()
    {
        Console.WriteLine();
        Console.WriteLine("=== ID Generation Collision Summary ===");
        Console.WriteLine($"Snowflake:     {SnowflakeIds.Count:N0} generated, {SnowflakeCollisions:N0} collisions");
        Console.WriteLine($"ULID:          {UlidIds.Count:N0} generated, {UlidCollisions:N0} collisions");
        Console.WriteLine($"UUIDv7:        {UuidV7Ids.Count:N0} generated, {UuidV7Collisions:N0} collisions");
        Console.WriteLine($"ShardPrefixed: {ShardPrefixedIds.Count:N0} generated, {ShardPrefixedCollisions:N0} collisions");
    }
}
