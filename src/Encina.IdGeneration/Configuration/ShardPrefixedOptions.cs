namespace Encina.IdGeneration.Configuration;

/// <summary>
/// Configuration options for the shard-prefixed ID generator.
/// </summary>
/// <remarks>
/// <para>
/// Controls the format of the unique portion appended after the shard prefix.
/// The generator produces IDs in the format <c>{shardId}{Delimiter}{sequence}</c>,
/// where the sequence portion is generated according to the selected <see cref="Format"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaIdGeneration(options =>
/// {
///     options.UseShardPrefixed(sp =>
///     {
///         sp.Format = ShardPrefixedFormat.Ulid;
///         sp.Delimiter = ':';
///     });
/// });
/// </code>
/// </example>
public sealed class ShardPrefixedOptions
{
    /// <summary>
    /// Gets or sets the format used for the sequence portion of the ID.
    /// </summary>
    /// <remarks>Default: <see cref="ShardPrefixedFormat.Ulid"/>.</remarks>
    public ShardPrefixedFormat Format { get; set; } = ShardPrefixedFormat.Ulid;

    /// <summary>
    /// Gets or sets the delimiter character separating the shard prefix from the sequence.
    /// </summary>
    /// <remarks>Default: <c>':'</c>.</remarks>
    public char Delimiter { get; set; } = ShardPrefixedId.Delimiter;
}

/// <summary>
/// Specifies the format for the sequence portion of a shard-prefixed ID.
/// </summary>
public enum ShardPrefixedFormat
{
    /// <summary>
    /// The sequence portion is a ULID (Crockford Base32, 26 characters).
    /// Produces IDs like <c>shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV</c>.
    /// </summary>
    Ulid,

    /// <summary>
    /// The sequence portion is a UUIDv7 (standard GUID format).
    /// Produces IDs like <c>shard-01:019374c8-7b00-7000-8000-000000000001</c>.
    /// </summary>
    UuidV7,

    /// <summary>
    /// The sequence portion is a numeric timestamp with random suffix.
    /// Produces IDs like <c>shard-01:1706745600000-a3f8</c>.
    /// </summary>
    TimestampRandom
}
