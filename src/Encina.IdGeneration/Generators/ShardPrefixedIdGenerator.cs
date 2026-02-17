using System.Globalization;
using System.Security.Cryptography;
using Encina.IdGeneration.Configuration;
using LanguageExt;

namespace Encina.IdGeneration.Generators;

/// <summary>
/// Generates shard-prefixed IDs in the format <c>{shardId}{delimiter}{sequence}</c>,
/// where the sequence portion is produced according to the configured <see cref="ShardPrefixedFormat"/>.
/// </summary>
/// <remarks>
/// <para>
/// This generator supports three sequence formats:
/// <list type="bullet">
/// <item><description><see cref="ShardPrefixedFormat.Ulid"/> — Crockford Base32 ULID (26 chars, time-ordered)</description></item>
/// <item><description><see cref="ShardPrefixedFormat.UuidV7"/> — RFC 9562 UUIDv7 (standard GUID format, time-ordered)</description></item>
/// <item><description><see cref="ShardPrefixedFormat.TimestampRandom"/> — Unix timestamp with random hex suffix</description></item>
/// </list>
/// </para>
/// <para>
/// Thread-safety: This generator is inherently thread-safe because each call produces
/// independent random/timestamp-based values. No shared mutable state is maintained.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new ShardPrefixedOptions { Format = ShardPrefixedFormat.Ulid, Delimiter = ':' };
/// var generator = new ShardPrefixedIdGenerator(options);
///
/// var result = generator.Generate("shard-01");
/// // → Right("shard-01:01ARZ3NDEKTSV4RRFFQ69G5FAV")
///
/// var shardId = generator.ExtractShardId(result.IfLeft(default(ShardPrefixedId)));
/// // → Right("shard-01")
/// </code>
/// </example>
public sealed class ShardPrefixedIdGenerator : IShardedIdGenerator<ShardPrefixedId>
{
    private readonly ShardPrefixedOptions _options;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShardPrefixedIdGenerator"/> class.
    /// </summary>
    /// <param name="options">The shard-prefixed ID configuration options.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testable time operations. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public ShardPrefixedIdGenerator(ShardPrefixedOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string StrategyName => "ShardPrefixed";

    /// <inheritdoc />
    /// <remarks>
    /// The parameterless <see cref="Generate()"/> is not supported for shard-prefixed IDs
    /// because a shard ID is required. Use <see cref="Generate(string)"/> instead.
    /// </remarks>
    public Either<EncinaError, ShardPrefixedId> Generate()
    {
        return IdGenerationErrors.InvalidShardId(
            null,
            "ShardPrefixedIdGenerator requires a shard ID. Use Generate(shardId) instead.");
    }

    /// <inheritdoc />
    public Either<EncinaError, ShardPrefixedId> Generate(string shardId)
    {
        if (string.IsNullOrWhiteSpace(shardId))
        {
            return IdGenerationErrors.InvalidShardId(shardId, "Shard ID cannot be null or whitespace.");
        }

        if (shardId.Contains(_options.Delimiter))
        {
            return IdGenerationErrors.InvalidShardId(
                shardId,
                $"Shard ID cannot contain the delimiter character '{_options.Delimiter}'.");
        }

        var timestamp = _timeProvider.GetUtcNow();
        var sequence = GenerateSequence(timestamp);

        return new ShardPrefixedId(shardId, sequence);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> ExtractShardId(ShardPrefixedId id)
    {
        if (id.IsEmpty)
        {
            return IdGenerationErrors.InvalidShardId(null, "Cannot extract shard ID from an empty ShardPrefixedId.");
        }

        return id.ShardId;
    }

    private string GenerateSequence(DateTimeOffset timestamp)
    {
        return _options.Format switch
        {
            ShardPrefixedFormat.Ulid => UlidId.NewUlid(timestamp).ToString(),
            ShardPrefixedFormat.UuidV7 => UuidV7Id.NewUuidV7(timestamp).ToString(),
            ShardPrefixedFormat.TimestampRandom => GenerateTimestampRandom(timestamp),
            _ => throw new InvalidOperationException($"Unsupported shard-prefixed format: {_options.Format}.")
        };
    }

    private static string GenerateTimestampRandom(DateTimeOffset timestamp)
    {
        var ms = timestamp.ToUnixTimeMilliseconds();
        Span<byte> randomBytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(randomBytes);
        var hex = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{ms}-{hex}");
    }
}
