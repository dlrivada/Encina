using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Encina.DomainModeling.Diagnostics;

/// <summary>
/// High-performance logging methods for repository operations using LoggerMessage source generators.
/// </summary>
/// <remarks>
/// <para>
/// Event IDs are allocated in the 1100-1199 range to avoid collisions with other Encina modules.
/// </para>
/// <para>
/// This class uses source generators to create optimized logging methods.
/// Excluded from code coverage as the generated code is boilerplate.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public static partial class RepositoryLog
{
    /// <summary>Logs when a repository operation begins execution.</summary>
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Debug,
        Message = "Executing repository operation {Operation} for {EntityType} using {Provider}")]
    public static partial void ExecutingOperation(
        ILogger logger,
        string operation,
        string entityType,
        string provider);

    /// <summary>Logs when a repository operation completes successfully.</summary>
    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Debug,
        Message = "Repository operation {Operation} for {EntityType} completed in {DurationMs}ms (ResultCount: {ResultCount})")]
    public static partial void OperationCompleted(
        ILogger logger,
        string operation,
        string entityType,
        long durationMs,
        int resultCount);

    /// <summary>Logs when a repository operation fails with a domain error.</summary>
    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Warning,
        Message = "Repository operation {Operation} for {EntityType} failed with error {ErrorCode}: {ErrorMessage}")]
    public static partial void OperationFailed(
        ILogger logger,
        string operation,
        string entityType,
        string errorCode,
        string errorMessage);

    /// <summary>Logs when a repository operation throws an unexpected exception.</summary>
    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Error,
        Message = "Repository operation {Operation} for {EntityType} threw an unexpected exception")]
    public static partial void OperationException(
        ILogger logger,
        Exception exception,
        string operation,
        string entityType);
}
