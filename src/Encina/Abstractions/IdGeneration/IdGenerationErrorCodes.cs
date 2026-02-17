namespace Encina;

/// <summary>
/// Error codes emitted by the Encina ID generation infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// All error codes follow the <c>encina.idgen.*</c> namespace convention and are returned
/// inside <c>Either&lt;EncinaError, T&gt;</c> results throughout the ID generation API. These
/// codes are also emitted as OpenTelemetry tags (<c>encina.idgen.error.code</c>) on generation
/// activity spans, enabling correlation between ROP error paths and distributed traces.
/// </para>
/// <para>
/// Error codes are stable string constants suitable for alerting rules, log filters, and
/// dashboard queries. They never change between releases.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Either&lt;EncinaError, SnowflakeId&gt; result = generator.Generate();
///
/// result.Match(
///     Right: id => logger.LogInformation("Generated ID {Id}", id),
///     Left: error =>
///     {
///         var code = error.GetCode();
///         code.IfSome(c =>
///         {
///             if (c == IdGenerationErrorCodes.ClockDriftDetected)
///                 logger.LogWarning("Clock drift detected, retrying...");
///             else if (c == IdGenerationErrorCodes.SequenceExhausted)
///                 logger.LogError("Sequence exhausted for current millisecond");
///         });
///     });
/// </code>
/// </example>
public static class IdGenerationErrorCodes
{
    /// <summary>
    /// The system clock moved backward, making timestamp-based ID generation unsafe.
    /// </summary>
    /// <remarks>
    /// This typically indicates NTP clock synchronization adjusted the system time.
    /// Snowflake generators should wait for the clock to catch up or fail fast.
    /// </remarks>
    public const string ClockDriftDetected = "encina.idgen.clock_drift_detected";

    /// <summary>
    /// The per-millisecond (or per-tick) sequence counter has been exhausted.
    /// </summary>
    /// <remarks>
    /// This occurs when more IDs are requested within a single time unit than the
    /// sequence bits can represent (e.g., &gt;4096 IDs per millisecond with 12-bit sequence).
    /// The caller should wait for the next time unit or use a generator with more sequence bits.
    /// </remarks>
    public const string SequenceExhausted = "encina.idgen.sequence_exhausted";

    /// <summary>
    /// The provided shard ID is invalid (null, empty, or out of range for the bit allocation).
    /// </summary>
    public const string InvalidShardId = "encina.idgen.invalid_shard_id";

    /// <summary>
    /// An ID string or value could not be parsed into the expected strongly-typed ID format.
    /// </summary>
    public const string IdParseFailure = "encina.idgen.id_parse_failure";
}
