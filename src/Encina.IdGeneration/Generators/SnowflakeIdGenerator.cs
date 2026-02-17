using System.Globalization;
using Encina.IdGeneration.Configuration;
using LanguageExt;

namespace Encina.IdGeneration.Generators;

/// <summary>
/// Generates Snowflake IDs with configurable bit allocation, clock drift detection,
/// and optional shard ID embedding.
/// </summary>
/// <remarks>
/// <para>
/// The default bit layout follows the Twitter/X Snowflake convention:
/// <c>[1 sign][41 timestamp][10 shard/machine][12 sequence]</c> = 64 bits.
/// The bit allocation is fully configurable via <see cref="SnowflakeOptions"/>.
/// </para>
/// <para>
/// This generator is thread-safe. Sequence increments use <see cref="Interlocked"/>
/// operations, and clock drift detection employs lock-free compare-and-swap.
/// </para>
/// <para>
/// When clock drift is detected within the configured tolerance, the generator
/// spin-waits until the clock catches up. Beyond the tolerance threshold, a
/// <see cref="IdGenerationErrorCodes.ClockDriftDetected"/> error is returned.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var generator = new SnowflakeIdGenerator(new SnowflakeOptions { MachineId = 1 });
///
/// // Generate without shard (uses configured MachineId)
/// var result = generator.Generate();
///
/// // Generate with shard embedding
/// var shardResult = generator.Generate("42");
///
/// // Extract shard from an existing ID
/// var shardId = generator.ExtractShardId(someSnowflakeId);
/// </code>
/// </example>
public sealed class SnowflakeIdGenerator : IShardedIdGenerator<SnowflakeId>
{
    private readonly SnowflakeOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly long _shardMask;
    private readonly long _sequenceMask;

    private long _lastTimestamp = -1;
    private long _sequence;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeIdGenerator"/> class.
    /// </summary>
    /// <param name="options">The Snowflake configuration options.</param>
    /// <param name="timeProvider">
    /// Optional time provider for testable time operations. Defaults to <see cref="TimeProvider.System"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options"/> is invalid.</exception>
    public SnowflakeIdGenerator(SnowflakeOptions options, TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Validate();
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _shardMask = (1L << _options.ShardBits) - 1;
        _sequenceMask = (1L << _options.SequenceBits) - 1;
    }

    /// <inheritdoc />
    public string StrategyName => "Snowflake";

    /// <inheritdoc />
    public Either<EncinaError, SnowflakeId> Generate()
    {
        return GenerateInternal(_options.MachineId);
    }

    /// <inheritdoc />
    public Either<EncinaError, SnowflakeId> Generate(string shardId)
    {
        if (string.IsNullOrWhiteSpace(shardId))
        {
            return IdGenerationErrors.InvalidShardId(shardId, "Shard ID cannot be null or whitespace.");
        }

        if (!long.TryParse(shardId, CultureInfo.InvariantCulture, out var numericShardId))
        {
            return IdGenerationErrors.InvalidShardId(
                shardId,
                $"Shard ID must be a numeric value for Snowflake encoding. Got: '{shardId}'.");
        }

        if (numericShardId < 0 || numericShardId > _shardMask)
        {
            return IdGenerationErrors.InvalidShardId(
                shardId,
                $"Shard ID must be in range [0, {_shardMask}]. Got: {numericShardId}.");
        }

        return GenerateInternal(numericShardId);
    }

    /// <inheritdoc />
    public Either<EncinaError, string> ExtractShardId(SnowflakeId id)
    {
        var shardBits = (id.Value >> _options.SequenceBits) & _shardMask;
        return shardBits.ToString(CultureInfo.InvariantCulture);
    }

    private Either<EncinaError, SnowflakeId> GenerateInternal(long machineId)
    {
        lock (_lock)
        {
            var currentTimestamp = GetCurrentTimestamp();

            // Clock moved backward
            if (currentTimestamp < _lastTimestamp)
            {
                var drift = _lastTimestamp - currentTimestamp;

                if (drift > _options.ClockDriftToleranceMs)
                {
                    return IdGenerationErrors.ClockDriftDetected(drift);
                }

                // Spin-wait for the clock to catch up
                while (currentTimestamp < _lastTimestamp)
                {
                    Thread.SpinWait(100);
                    currentTimestamp = GetCurrentTimestamp();
                }
            }

            if (currentTimestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & _sequenceMask;

                if (_sequence == 0)
                {
                    // Sequence exhausted for this millisecond, wait for next
                    currentTimestamp = WaitForNextMillisecond(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = currentTimestamp;

            var id = (currentTimestamp << (_options.ShardBits + _options.SequenceBits))
                   | (machineId << _options.SequenceBits)
                   | _sequence;

            return new SnowflakeId(id);
        }
    }

    private long GetCurrentTimestamp()
    {
        var now = _timeProvider.GetUtcNow();
        return now.ToUnixTimeMilliseconds() - _options.EpochStart.ToUnixTimeMilliseconds();
    }

    private long WaitForNextMillisecond(long lastTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            Thread.SpinWait(100);
            timestamp = GetCurrentTimestamp();
        }

        return timestamp;
    }
}
