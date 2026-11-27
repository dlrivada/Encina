using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.IdGeneration.Diagnostics;

/// <summary>
/// High-performance logging methods for ID generation using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 12100-12199 range reserved for ID generation
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
    // ID Generation (12100-12109)
    [LoggerMessage(
        EventId = 12100,
        Level = LogLevel.Debug,
        Message = "Generated {Strategy} ID: {IdValue} (ShardId: {ShardId})")]
    public static partial void IdGenerated(
        ILogger logger,
        string strategy,
        string idValue,
        string? shardId);

    // Sequence Exhaustion (12110-12119)
    [LoggerMessage(
        EventId = 12110,
        Level = LogLevel.Warning,
        Message = "Snowflake sequence exhausted for timestamp {Timestamp}ms, waiting for next millisecond")]
    public static partial void SequenceExhausted(
        ILogger logger,
        long timestamp);

    // Clock Drift (12120-12129)
    [LoggerMessage(
        EventId = 12120,
        Level = LogLevel.Warning,
        Message = "Clock drift detected: {DriftMs}ms (tolerance: {ToleranceMs}ms)")]
    public static partial void ClockDriftDetected(
        ILogger logger,
        long driftMs,
        long toleranceMs);
}
