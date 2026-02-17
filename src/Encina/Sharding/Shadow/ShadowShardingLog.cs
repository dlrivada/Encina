using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Sharding.Shadow;

/// <summary>
/// High-performance log messages for shadow sharding operations.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="LoggerMessageAttribute"/> source generators for zero-allocation logging.
/// EventId range: 700-749 (reserved for shadow sharding).
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

    [LoggerMessage(EventId = 700, Level = LogLevel.Warning,
        Message = "Shadow routing failed for shard key '{ShardKey}': {ErrorMessage}")]
    public static partial void ShadowRoutingFailed(ILogger logger, string shardKey, string errorMessage);

    [LoggerMessage(EventId = 701, Level = LogLevel.Warning,
        Message = "Shadow routing mismatch for shard key '{ShardKey}': production={ProductionShardId}, shadow={ShadowShardId}")]
    public static partial void RoutingMismatch(ILogger logger, string shardKey, string productionShardId, string shadowShardId);

    // ── Shadow write pipeline (710-719) ─────────────────────────────────

    [LoggerMessage(EventId = 710, Level = LogLevel.Warning,
        Message = "Shadow write failed for command '{CommandType}': {ErrorMessage}")]
    public static partial void ShadowWriteFailed(ILogger logger, string commandType, string errorMessage);

    [LoggerMessage(EventId = 711, Level = LogLevel.Warning,
        Message = "Shadow write timed out for command '{CommandType}' after {TimeoutMs}ms")]
    public static partial void ShadowWriteTimedOut(ILogger logger, string commandType, double timeoutMs);

    // ── Shadow read pipeline (720-729) ──────────────────────────────────

    [LoggerMessage(EventId = 720, Level = LogLevel.Warning,
        Message = "Shadow read discrepancy for query '{QueryType}': production hash={ProductionHash}, shadow hash={ShadowHash}")]
    public static partial void ShadowReadDiscrepancy(ILogger logger, string queryType, int productionHash, int shadowHash);

    [LoggerMessage(EventId = 721, Level = LogLevel.Warning,
        Message = "Shadow read failed for query '{QueryType}': {ErrorMessage}")]
    public static partial void ShadowReadFailed(ILogger logger, string queryType, string errorMessage);

    [LoggerMessage(EventId = 722, Level = LogLevel.Warning,
        Message = "Shadow discrepancy handler failed for query '{QueryType}': {ErrorMessage}")]
    public static partial void DiscrepancyHandlerFailed(ILogger logger, string queryType, string errorMessage);

    // ── Shadow lifecycle (730-739) ───────────────────────────────────────

    [LoggerMessage(EventId = 730, Level = LogLevel.Information,
        Message = "Shadow sharding enabled: topology={TopologyDescription}, dualWrite={DualWriteEnabled}, readPercentage={ShadowReadPercentage}%")]
    public static partial void ShadowShardingEnabled(ILogger logger, string topologyDescription, bool dualWriteEnabled, int shadowReadPercentage);

    [LoggerMessage(EventId = 731, Level = LogLevel.Information,
        Message = "Shadow comparison summary: total={TotalComparisons}, mismatchRate={MismatchRate:F2}%, avgLatencyDiffMs={AvgLatencyDiffMs:F1}")]
    public static partial void ShadowComparisonSummary(ILogger logger, long totalComparisons, double mismatchRate, double avgLatencyDiffMs);
}
