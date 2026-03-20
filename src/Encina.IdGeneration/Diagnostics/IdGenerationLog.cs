using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.IdGeneration.Diagnostics;

/// <summary>
/// High-performance logging methods for ID generation using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 8030-8099 range reserved for ID generation
/// (see <c>EventIdRanges.IdGeneration</c>).
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class IdGenerationLog
{
    // ID Generation (8030-8039)
    [LoggerMessage(
        EventId = 8030,
        Level = LogLevel.Debug,
        Message = "Generated {Strategy} ID: {IdValue} (ShardId: {ShardId})")]
    public static partial void IdGenerated(
        ILogger logger,
        string strategy,
        string idValue,
        string? shardId);

    // Sequence Exhaustion (8040-8049)
    [LoggerMessage(
        EventId = 8040,
        Level = LogLevel.Warning,
        Message = "Snowflake sequence exhausted for timestamp {Timestamp}ms, waiting for next millisecond")]
    public static partial void SequenceExhausted(
        ILogger logger,
        long timestamp);

    // Clock Drift (8050-8059)
    [LoggerMessage(
        EventId = 8050,
        Level = LogLevel.Warning,
        Message = "Clock drift detected: {DriftMs}ms (tolerance: {ToleranceMs}ms)")]
    public static partial void ClockDriftDetected(
        ILogger logger,
        long driftMs,
        long toleranceMs);
}
