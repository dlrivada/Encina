using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow;

/// <summary>
/// High-performance log messages for shadow sharding operations.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="LoggerMessageAttribute"/> source generators for zero-allocation logging.
/// EventIds 138-146, inside <c>EventIdRanges.Core</c>.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class ShadowShardingLog
{
    // ── Shadow routing (700-709) ────────────────────────────────────────

    [LoggerMessage(EventId = 138, Level = LogLevel.Warning,
        Message = "Shadow routing failed for shard key '{ShardKey}': {ErrorCode}")]
    public static partial void ShadowRoutingFailed(ILogger logger, string shardKey, string errorCode, Exception? exception);

    [LoggerMessage(EventId = 139, Level = LogLevel.Warning,
        Message = "Shadow routing mismatch for shard key '{ShardKey}': production={ProductionShardId}, shadow={ShadowShardId}")]
    public static partial void RoutingMismatch(ILogger logger, string shardKey, string productionShardId, string shadowShardId);

    // ── Shadow write pipeline (710-719) ─────────────────────────────────

    [LoggerMessage(EventId = 140, Level = LogLevel.Warning,
        Message = "Shadow write failed for command '{CommandType}': {ErrorCode}")]
    public static partial void ShadowWriteFailed(ILogger logger, string commandType, string errorCode, Exception? exception);

    [LoggerMessage(EventId = 141, Level = LogLevel.Warning,
        Message = "Shadow write timed out for command '{CommandType}' after {TimeoutMs}ms")]
    public static partial void ShadowWriteTimedOut(ILogger logger, string commandType, double timeoutMs);

    // ── Shadow read pipeline (720-729) ──────────────────────────────────

    [LoggerMessage(EventId = 142, Level = LogLevel.Warning,
        Message = "Shadow read discrepancy for query '{QueryType}': production hash={ProductionHash}, shadow hash={ShadowHash}")]
    public static partial void ShadowReadDiscrepancy(ILogger logger, string queryType, int productionHash, int shadowHash);

    [LoggerMessage(EventId = 143, Level = LogLevel.Warning,
        Message = "Shadow read failed for query '{QueryType}': {ErrorCode}")]
    public static partial void ShadowReadFailed(ILogger logger, string queryType, string errorCode, Exception? exception);

    [LoggerMessage(EventId = 144, Level = LogLevel.Warning,
        Message = "Shadow discrepancy handler failed for query '{QueryType}': {ErrorCode}")]
    public static partial void DiscrepancyHandlerFailed(ILogger logger, string queryType, string errorCode, Exception? exception);

    // ── Shadow lifecycle (730-739) ───────────────────────────────────────

    [LoggerMessage(EventId = 145, Level = LogLevel.Information,
        Message = "Shadow sharding enabled: topology={TopologyDescription}, dualWrite={DualWriteEnabled}, readPercentage={ShadowReadPercentage}%")]
    public static partial void ShadowShardingEnabled(ILogger logger, string topologyDescription, bool dualWriteEnabled, int shadowReadPercentage);

    [LoggerMessage(EventId = 146, Level = LogLevel.Information,
        Message = "Shadow comparison summary: total={TotalComparisons}, mismatchRate={MismatchRate:F2}%, avgLatencyDiffMs={AvgLatencyDiffMs:F1}")]
    public static partial void ShadowComparisonSummary(ILogger logger, long totalComparisons, double mismatchRate, double avgLatencyDiffMs);
}
