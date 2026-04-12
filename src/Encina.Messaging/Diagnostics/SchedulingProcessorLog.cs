using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for the <c>ScheduledMessageProcessor</c> background
/// service using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 2320-2349 sub-range within the already-registered
/// <c>MessagingScheduling</c> (2300-2399) range. No <c>EventIdRanges.cs</c> change
/// is required because the sub-range fits within the parent allocation.
/// </para>
/// <para>
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class SchedulingProcessorLog
{
    /// <summary>Logs when the processor is starting with its configuration.</summary>
    [LoggerMessage(
        EventId = 2320,
        Level = LogLevel.Information,
        Message = "ScheduledMessageProcessor starting. Interval={Interval}, BatchSize={BatchSize}, MaxRetries={MaxRetries}")]
    public static partial void ProcessorStarting(
        ILogger logger,
        TimeSpan interval,
        int batchSize,
        int maxRetries);

    /// <summary>Logs when the processor is stopping.</summary>
    [LoggerMessage(
        EventId = 2321,
        Level = LogLevel.Information,
        Message = "ScheduledMessageProcessor stopping")]
    public static partial void ProcessorStopping(ILogger logger);

    /// <summary>Logs when the processor is disabled by configuration.</summary>
    [LoggerMessage(
        EventId = 2322,
        Level = LogLevel.Information,
        Message = "ScheduledMessageProcessor is disabled (SchedulingOptions.EnableProcessor = false)")]
    public static partial void ProcessorDisabled(ILogger logger);

    /// <summary>Logs when a processing cycle completes with dispatched messages.</summary>
    [LoggerMessage(
        EventId = 2323,
        Level = LogLevel.Debug,
        Message = "ScheduledMessageProcessor batch completed. Processed={ProcessedCount}")]
    public static partial void BatchCompleted(
        ILogger logger,
        int processedCount);

    /// <summary>Logs when a processing cycle fails at the store retrieval level.</summary>
    [LoggerMessage(
        EventId = 2324,
        Level = LogLevel.Warning,
        Message = "ScheduledMessageProcessor batch failed: [{ErrorCode}] {ErrorMessage}")]
    public static partial void BatchFailed(
        ILogger logger,
        string errorCode,
        string errorMessage);

    /// <summary>Logs when a processing cycle throws an unhandled exception (safety net).</summary>
    [LoggerMessage(
        EventId = 2325,
        Level = LogLevel.Error,
        Message = "ScheduledMessageProcessor cycle threw an unhandled exception")]
    public static partial void CycleFailed(
        ILogger logger,
        Exception exception);
}
