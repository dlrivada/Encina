using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.Messaging.Diagnostics;

/// <summary>
/// High-performance logging methods for the message purge service using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs: 2107-2111 (see <c>EventIdRanges.MessagingInbox</c>, 2100-2199). No message carries the text of
/// an error or an exception: only the error code or the exception type reaches the log.
/// </para>
/// <para>
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
internal static partial class MessagePurgeLog
{
    /// <summary>Logs when a purge loop starts.</summary>
    [LoggerMessage(
        EventId = 2107,
        Level = LogLevel.Information,
        Message = "Message purge for {Pattern} started: every {Interval}, in batches of {BatchSize}")]
    internal static partial void Started(
        ILogger logger,
        string pattern,
        TimeSpan interval,
        int batchSize);

    /// <summary>Logs when the service has no pattern with its automatic purge on and does nothing.</summary>
    [LoggerMessage(
        EventId = 2108,
        Level = LogLevel.Information,
        Message = "Message purge service has no pattern with automatic purge enabled and will not run")]
    internal static partial void NothingToPurge(ILogger logger);

    /// <summary>Logs how many messages one cycle purged.</summary>
    [LoggerMessage(
        EventId = 2109,
        Level = LogLevel.Information,
        Message = "Message purge for {Pattern} removed {Count} expired messages")]
    internal static partial void Purged(
        ILogger logger,
        string pattern,
        int count);

    /// <summary>Logs when a store returned an error during a purge cycle; the next cycle still runs.</summary>
    [LoggerMessage(
        EventId = 2110,
        Level = LogLevel.Warning,
        Message = "Message purge cycle for {Pattern} failed with error code {ErrorCode}")]
    internal static partial void CycleFailed(
        ILogger logger,
        string pattern,
        string errorCode);

    /// <summary>Logs when a purge cycle threw an unexpected exception; the next cycle still runs.</summary>
    [LoggerMessage(
        EventId = 2111,
        Level = LogLevel.Error,
        Message = "Message purge cycle for {Pattern} threw an unexpected exception")]
    internal static partial void CycleException(
        ILogger logger,
        string pattern,
        Exception exception);
}
