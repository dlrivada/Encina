namespace Encina.IdGeneration.Configuration;

/// <summary>
/// Configuration options for the Snowflake ID generator.
/// </summary>
/// <remarks>
/// <para>
/// The Snowflake algorithm encodes a timestamp, machine/shard identifier, and sequence
/// counter in a single 64-bit integer. The default bit layout follows the Twitter/X
/// convention: 41 bits timestamp + 10 bits machine/shard + 12 bits sequence = 63 bits
/// (sign bit is always 0).
/// </para>
/// <para>
/// The bit allocation is configurable to support different trade-offs:
/// <list type="bullet">
/// <item><description>More timestamp bits → longer epoch before overflow</description></item>
/// <item><description>More shard bits → more unique shards/machines</description></item>
/// <item><description>More sequence bits → more IDs per millisecond</description></item>
/// </list>
/// Total bits must sum to exactly 63 (sign bit excluded).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaIdGeneration(options =>
/// {
///     options.UseSnowflake(snowflake =>
///     {
///         snowflake.MachineId = 1;
///         snowflake.EpochStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
///         snowflake.ClockDriftToleranceMs = 5;
///     });
/// });
/// </code>
/// </example>
public sealed class SnowflakeOptions
{
    /// <summary>
    /// Gets or sets the machine/shard identifier for this instance.
    /// Must be in the range [0, 2^<see cref="ShardBits"/> - 1].
    /// </summary>
    /// <remarks>Default: 0.</remarks>
    public long MachineId { get; set; }

    /// <summary>
    /// Gets or sets the custom epoch start for timestamp calculation.
    /// </summary>
    /// <remarks>
    /// Default: 2024-01-01T00:00:00Z. A more recent epoch means the timestamp bits
    /// last longer before overflow (~69 years with 41 bits from epoch).
    /// </remarks>
    public DateTimeOffset EpochStart { get; set; } = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Gets or sets the number of bits allocated for the timestamp portion.
    /// </summary>
    /// <remarks>
    /// Default: 41 bits (~69 years from epoch at millisecond precision).
    /// Must satisfy: <see cref="TimestampBits"/> + <see cref="ShardBits"/> + <see cref="SequenceBits"/> == 63.
    /// </remarks>
    public int TimestampBits { get; set; } = 41;

    /// <summary>
    /// Gets or sets the number of bits allocated for the shard/machine identifier.
    /// </summary>
    /// <remarks>
    /// Default: 10 bits (1024 unique shards/machines).
    /// Must satisfy: <see cref="TimestampBits"/> + <see cref="ShardBits"/> + <see cref="SequenceBits"/> == 63.
    /// </remarks>
    public int ShardBits { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of bits allocated for the per-millisecond sequence counter.
    /// </summary>
    /// <remarks>
    /// Default: 12 bits (4096 IDs per millisecond per shard).
    /// Must satisfy: <see cref="TimestampBits"/> + <see cref="ShardBits"/> + <see cref="SequenceBits"/> == 63.
    /// </remarks>
    public int SequenceBits { get; set; } = 12;

    /// <summary>
    /// Gets or sets the maximum allowed clock drift in milliseconds before
    /// the generator returns an error instead of waiting.
    /// </summary>
    /// <remarks>
    /// Default: 5ms. When the system clock moves backward by less than this threshold,
    /// the generator will spin-wait until the clock catches up. Beyond this threshold,
    /// an <see cref="IdGenerationErrorCodes.ClockDriftDetected"/> error is returned.
    /// </remarks>
    public long ClockDriftToleranceMs { get; set; } = 5;

    /// <summary>
    /// Gets the maximum value for the machine/shard ID based on the current bit allocation.
    /// </summary>
    public long MaxMachineId => (1L << ShardBits) - 1;

    /// <summary>
    /// Gets the maximum value for the per-millisecond sequence counter.
    /// </summary>
    public long MaxSequence => (1L << SequenceBits) - 1;

    /// <summary>
    /// Validates the current options, throwing if invalid.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the options are invalid.</exception>
    internal void Validate()
    {
        if (TimestampBits + ShardBits + SequenceBits != 63)
        {
            throw new ArgumentException(
                $"Bit allocation must sum to 63. Current: {TimestampBits} + {ShardBits} + {SequenceBits} = " +
                $"{TimestampBits + ShardBits + SequenceBits}.");
        }

        if (TimestampBits < 1 || ShardBits < 0 || SequenceBits < 1)
        {
            throw new ArgumentException(
                "TimestampBits and SequenceBits must be >= 1, ShardBits must be >= 0.");
        }

        if (MachineId < 0 || MachineId > MaxMachineId)
        {
            throw new ArgumentException(
                $"MachineId must be in range [0, {MaxMachineId}]. Got: {MachineId}.");
        }
    }
}
