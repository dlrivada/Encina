using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.ScatterGather;

/// <summary>
/// High-performance logging for scatter-gather operations.
/// </summary>
/// <remarks>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class ScatterGatherLog
{
    [LoggerMessage(
        EventId = 600,
        Level = LogLevel.Information,
        Message = "Scatter-gather '{OperationName}' [{OperationId}] started with {ScatterCount} handlers (Strategy: {Strategy})")]
    public static partial void ExecutionStarted(
        ILogger logger,
        string operationName,
        Guid operationId,
        int scatterCount,
        GatherStrategy strategy);

    [LoggerMessage(
        EventId = 601,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] executing scatter handler '{HandlerName}'")]
    public static partial void ScatterExecuting(
        ILogger logger,
        Guid operationId,
        string handlerName);

    [LoggerMessage(
        EventId = 602,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] scatter handler '{HandlerName}' completed successfully in {Duration}")]
    public static partial void ScatterCompleted(
        ILogger logger,
        Guid operationId,
        string handlerName,
        TimeSpan duration);

    [LoggerMessage(
        EventId = 603,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] scatter handler '{HandlerName}' failed: {ErrorMessage}")]
    public static partial void ScatterFailed(
        ILogger logger,
        Guid operationId,
        string handlerName,
        string errorMessage);

    [LoggerMessage(
        EventId = 604,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] scatter handler '{HandlerName}' was cancelled")]
    public static partial void ScatterCancelled(
        ILogger logger,
        Guid operationId,
        string handlerName);

    [LoggerMessage(
        EventId = 605,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] quorum reached ({QuorumCount}/{ScatterCount}), cancelling remaining handlers")]
    public static partial void QuorumReached(
        ILogger logger,
        Guid operationId,
        int quorumCount,
        int scatterCount);

    [LoggerMessage(
        EventId = 606,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] first result received from '{HandlerName}', cancelling remaining handlers")]
    public static partial void FirstResultReceived(
        ILogger logger,
        Guid operationId,
        string handlerName);

    [LoggerMessage(
        EventId = 607,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] executing gather handler with {ResultCount} results")]
    public static partial void GatherExecuting(
        ILogger logger,
        Guid operationId,
        int resultCount);

    [LoggerMessage(
        EventId = 608,
        Level = LogLevel.Debug,
        Message = "Scatter-gather [{OperationId}] gather handler completed successfully")]
    public static partial void GatherCompleted(
        ILogger logger,
        Guid operationId);

    [LoggerMessage(
        EventId = 609,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] gather handler failed: {ErrorMessage}")]
    public static partial void GatherFailed(
        ILogger logger,
        Guid operationId,
        string errorMessage);

    [LoggerMessage(
        EventId = 610,
        Level = LogLevel.Information,
        Message = "Scatter-gather '{OperationName}' [{OperationId}] completed in {Duration} ({SuccessCount}/{ScatterCount} succeeded)")]
    public static partial void ExecutionCompleted(
        ILogger logger,
        string operationName,
        Guid operationId,
        TimeSpan duration,
        int successCount,
        int scatterCount);

    [LoggerMessage(
        EventId = 611,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] was cancelled")]
    public static partial void ExecutionCancelled(
        ILogger logger,
        Guid operationId);

    [LoggerMessage(
        EventId = 612,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] timed out after {Timeout}")]
    public static partial void ExecutionTimedOut(
        ILogger logger,
        Guid operationId,
        TimeSpan timeout);

    [LoggerMessage(
        EventId = 613,
        Level = LogLevel.Error,
        Message = "Scatter-gather [{OperationId}] failed with exception: {ErrorMessage}")]
    public static partial void ExecutionException(
        ILogger logger,
        Guid operationId,
        string errorMessage,
        Exception exception);

    [LoggerMessage(
        EventId = 614,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] all scatter handlers failed")]
    public static partial void AllScattersFailed(
        ILogger logger,
        Guid operationId);

    [LoggerMessage(
        EventId = 615,
        Level = LogLevel.Warning,
        Message = "Scatter-gather [{OperationId}] quorum not reached ({SuccessCount}/{QuorumCount} required)")]
    public static partial void QuorumNotReached(
        ILogger logger,
        Guid operationId,
        int successCount,
        int quorumCount);
}
