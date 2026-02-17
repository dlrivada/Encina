using Microsoft.Extensions.Logging;

namespace Encina.OpenTelemetry.Resharding;

/// <summary>
/// High-performance structured log messages for the resharding workflow.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="LoggerMessage.Define"/> to avoid boxing and string formatting overhead
/// in hot paths. All methods are extension methods on <see cref="ILogger"/> for ergonomic use.
/// </para>
/// <para>
/// Event IDs are allocated in the 7000–7099 range to avoid collisions with other
/// Encina subsystems.
/// </para>
/// </remarks>
public static partial class ReshardingLogMessages
{
    // -- 7000: Resharding lifecycle --

    private static readonly Action<ILogger, Guid, int, long, Exception?> ReshardingStartedDef =
        LoggerMessage.Define<Guid, int, long>(
            LogLevel.Information,
            new EventId(7000, nameof(ReshardingStarted)),
            "Resharding started. ReshardingId={ReshardingId}, Steps={StepCount}, EstimatedRows={EstimatedRows}");

    /// <summary>
    /// Logs that a resharding operation has started.
    /// </summary>
    public static void ReshardingStarted(this ILogger logger, Guid reshardingId, int stepCount, long estimatedRows)
        => ReshardingStartedDef(logger, reshardingId, stepCount, estimatedRows, null);

    // -- 7001: Phase started --

    private static readonly Action<ILogger, Guid, string, Exception?> ReshardingPhaseStartedDef =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(7001, nameof(ReshardingPhaseStarted)),
            "Resharding phase starting. ReshardingId={ReshardingId}, Phase={Phase}");

    /// <summary>
    /// Logs that a resharding phase is starting.
    /// </summary>
    public static void ReshardingPhaseStarted(this ILogger logger, Guid reshardingId, string phase)
        => ReshardingPhaseStartedDef(logger, reshardingId, phase, null);

    // -- 7002: Phase completed --

    private static readonly Action<ILogger, Guid, string, double, Exception?> ReshardingPhaseCompletedDef =
        LoggerMessage.Define<Guid, string, double>(
            LogLevel.Information,
            new EventId(7002, nameof(ReshardingPhaseCompleted)),
            "Resharding phase completed. ReshardingId={ReshardingId}, Phase={Phase}, DurationMs={DurationMs:F1}");

    /// <summary>
    /// Logs that a resharding phase has completed.
    /// </summary>
    public static void ReshardingPhaseCompleted(this ILogger logger, Guid reshardingId, string phase, double durationMs)
        => ReshardingPhaseCompletedDef(logger, reshardingId, phase, durationMs, null);

    // -- 7003: Copy progress --

    private static readonly Action<ILogger, Guid, long, double, Exception?> ReshardingCopyProgressDef =
        LoggerMessage.Define<Guid, long, double>(
            LogLevel.Information,
            new EventId(7003, nameof(ReshardingCopyProgress)),
            "Resharding copy progress. ReshardingId={ReshardingId}, RowsCopied={RowsCopied}, PercentComplete={PercentComplete:F1}");

    /// <summary>
    /// Logs copy phase progress.
    /// </summary>
    public static void ReshardingCopyProgress(this ILogger logger, Guid reshardingId, long rowsCopied, double percentComplete)
        => ReshardingCopyProgressDef(logger, reshardingId, rowsCopied, percentComplete, null);

    // -- 7004: CDC lag update --

    private static readonly Action<ILogger, Guid, double, Exception?> ReshardingCdcLagUpdateDef =
        LoggerMessage.Define<Guid, double>(
            LogLevel.Information,
            new EventId(7004, nameof(ReshardingCdcLagUpdate)),
            "Resharding CDC lag update. ReshardingId={ReshardingId}, LagMs={LagMs:F1}");

    /// <summary>
    /// Logs the current CDC replication lag.
    /// </summary>
    public static void ReshardingCdcLagUpdate(this ILogger logger, Guid reshardingId, double lagMs)
        => ReshardingCdcLagUpdateDef(logger, reshardingId, lagMs, null);

    // -- 7005: Verification result --

    private static readonly Action<ILogger, Guid, bool, long, Exception?> ReshardingVerificationResultDef =
        LoggerMessage.Define<Guid, bool, long>(
            LogLevel.Information,
            new EventId(7005, nameof(ReshardingVerificationResult)),
            "Resharding verification result. ReshardingId={ReshardingId}, Matched={Matched}, MismatchCount={MismatchCount}");

    /// <summary>
    /// Logs the data verification result.
    /// </summary>
    public static void ReshardingVerificationResult(this ILogger logger, Guid reshardingId, bool matched, long mismatchCount)
        => ReshardingVerificationResultDef(logger, reshardingId, matched, mismatchCount, null);

    // -- 7006: Cutover started --

    private static readonly Action<ILogger, Guid, Exception?> ReshardingCutoverStartedDef =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(7006, nameof(ReshardingCutoverStarted)),
            "Resharding cutover starting (brief read-only window). ReshardingId={ReshardingId}");

    /// <summary>
    /// Logs that the cutover phase is starting (triggers a brief read-only window).
    /// </summary>
    public static void ReshardingCutoverStarted(this ILogger logger, Guid reshardingId)
        => ReshardingCutoverStartedDef(logger, reshardingId, null);

    // -- 7007: Cutover completed --

    private static readonly Action<ILogger, Guid, double, Exception?> ReshardingCutoverCompletedDef =
        LoggerMessage.Define<Guid, double>(
            LogLevel.Information,
            new EventId(7007, nameof(ReshardingCutoverCompleted)),
            "Resharding cutover completed. ReshardingId={ReshardingId}, CutoverDurationMs={CutoverDurationMs:F1}");

    /// <summary>
    /// Logs that the cutover phase has completed.
    /// </summary>
    public static void ReshardingCutoverCompleted(this ILogger logger, Guid reshardingId, double cutoverDurationMs)
        => ReshardingCutoverCompletedDef(logger, reshardingId, cutoverDurationMs, null);

    // -- 7008: Resharding failed --

    private static readonly Action<ILogger, Guid, string, Exception?> ReshardingFailedDef =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Error,
            new EventId(7008, nameof(ReshardingFailed)),
            "Resharding failed. ReshardingId={ReshardingId}, ErrorCode={ErrorCode}");

    /// <summary>
    /// Logs that a resharding operation has failed.
    /// </summary>
    public static void ReshardingFailed(this ILogger logger, Guid reshardingId, string errorCode)
        => ReshardingFailedDef(logger, reshardingId, errorCode, null);

    // -- 7009: Resharding rolled back --

    private static readonly Action<ILogger, Guid, string, Exception?> ReshardingRolledBackDef =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Warning,
            new EventId(7009, nameof(ReshardingRolledBack)),
            "Resharding rolled back. ReshardingId={ReshardingId}, LastCompletedPhase={LastCompletedPhase}");

    /// <summary>
    /// Logs that a resharding operation has been rolled back.
    /// </summary>
    public static void ReshardingRolledBack(this ILogger logger, Guid reshardingId, string lastCompletedPhase)
        => ReshardingRolledBackDef(logger, reshardingId, lastCompletedPhase, null);
}
