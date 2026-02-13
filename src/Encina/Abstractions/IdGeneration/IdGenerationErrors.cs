namespace Encina;

/// <summary>
/// Factory methods for ID generation errors, providing pre-built
/// <see cref="EncinaError"/> instances with appropriate error codes and messages.
/// </summary>
/// <remarks>
/// <para>
/// Each factory method creates an error using <see cref="EncinaErrors.Create"/> with
/// the corresponding <see cref="IdGenerationErrorCodes"/> code, ensuring consistent
/// error metadata for OpenTelemetry correlation and log filtering.
/// </para>
/// </remarks>
public static class IdGenerationErrors
{
    /// <summary>
    /// Creates an error indicating that the system clock moved backward.
    /// </summary>
    /// <param name="driftMilliseconds">The detected clock drift in milliseconds.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="IdGenerationErrorCodes.ClockDriftDetected"/>.</returns>
    public static EncinaError ClockDriftDetected(long driftMilliseconds)
        => EncinaErrors.Create(
            IdGenerationErrorCodes.ClockDriftDetected,
            $"Clock drift detected: system clock moved backward by {driftMilliseconds}ms. " +
            "Timestamp-based ID generation is unsafe until the clock catches up.");

    /// <summary>
    /// Creates an error indicating the per-time-unit sequence has been exhausted.
    /// </summary>
    /// <param name="maxSequence">The maximum sequence value for the current configuration.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="IdGenerationErrorCodes.SequenceExhausted"/>.</returns>
    public static EncinaError SequenceExhausted(long maxSequence)
        => EncinaErrors.Create(
            IdGenerationErrorCodes.SequenceExhausted,
            $"Sequence exhausted: exceeded maximum of {maxSequence} IDs in the current time unit. " +
            "Wait for the next time unit or increase sequence bit allocation.");

    /// <summary>
    /// Creates an error indicating the shard ID is invalid.
    /// </summary>
    /// <param name="shardId">The invalid shard ID value.</param>
    /// <param name="reason">A description of why the shard ID is invalid.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="IdGenerationErrorCodes.InvalidShardId"/>.</returns>
    public static EncinaError InvalidShardId(string? shardId, string reason)
        => EncinaErrors.Create(
            IdGenerationErrorCodes.InvalidShardId,
            $"Invalid shard ID '{shardId ?? "(null)"}': {reason}");

    /// <summary>
    /// Creates an error indicating an ID value could not be parsed.
    /// </summary>
    /// <param name="value">The string representation of the value that failed to parse.</param>
    /// <param name="targetType">The target ID type name.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="IdGenerationErrorCodes.IdParseFailure"/>.</returns>
    public static EncinaError IdParseFailure(string? value, string targetType)
        => EncinaErrors.Create(
            IdGenerationErrorCodes.IdParseFailure,
            $"Failed to parse '{value ?? "(null)"}' as {targetType}.");

    /// <summary>
    /// Creates an error indicating an ID value could not be parsed, with an inner exception.
    /// </summary>
    /// <param name="value">The string representation of the value that failed to parse.</param>
    /// <param name="targetType">The target ID type name.</param>
    /// <param name="exception">The exception that caused the parse failure.</param>
    /// <returns>An <see cref="EncinaError"/> with code <see cref="IdGenerationErrorCodes.IdParseFailure"/>.</returns>
    public static EncinaError IdParseFailure(string? value, string targetType, Exception exception)
        => EncinaErrors.Create(
            IdGenerationErrorCodes.IdParseFailure,
            $"Failed to parse '{value ?? "(null)"}' as {targetType}: {exception.Message}",
            exception);
}
