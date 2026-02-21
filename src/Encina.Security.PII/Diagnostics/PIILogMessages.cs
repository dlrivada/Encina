using Microsoft.Extensions.Logging;

namespace Encina.Security.PII.Diagnostics;

/// <summary>
/// High-performance structured log messages for PII masking using
/// <see cref="LoggerMessageAttribute"/>-generated methods.
/// </summary>
/// <remarks>
/// Uses compile-time source generation for zero-allocation logging when the
/// log level is not enabled. Each method corresponds to a specific masking event.
/// EventIds are in the 8000–8099 range reserved for PII diagnostics.
/// </remarks>
internal static partial class PIILogMessages
{
    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Debug,
        Message = "Starting PII masking for {EntityType} ({PropertyCount} properties)")]
    internal static partial void PIIMaskingStarted(
        ILogger logger, string entityType, int propertyCount);

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Debug,
        Message = "PII masking completed for {EntityType} ({MaskedCount} masked) in {ElapsedMs:F2}ms")]
    internal static partial void PIIMaskingCompleted(
        ILogger logger, string entityType, int maskedCount, double elapsedMs);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Warning,
        Message = "PII masking failed for {EntityType}: {ErrorMessage}")]
    internal static partial void PIIMaskingFailed(
        ILogger logger, string entityType, string errorMessage);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Trace,
        Message = "Strategy applied for property '{PropertyName}' (PIIType={PIIType}, Strategy={Strategy})")]
    internal static partial void StrategyApplied(
        ILogger logger, string propertyName, string piiType, string strategy);

    [LoggerMessage(
        EventId = 8004,
        Level = LogLevel.Warning,
        Message = "No masking strategy found for PIIType={PIIType}")]
    internal static partial void StrategyNotFound(
        ILogger logger, string piiType);

    [LoggerMessage(
        EventId = 8005,
        Level = LogLevel.Error,
        Message = "Serialization failed during PII masking for {EntityType}")]
    internal static partial void SerializationFailed(
        ILogger logger, Exception exception, string entityType);

    [LoggerMessage(
        EventId = 8006,
        Level = LogLevel.Debug,
        Message = "PII pipeline masking applied to response type {ResponseType}")]
    internal static partial void PipelineMaskingApplied(
        ILogger logger, string responseType);

    [LoggerMessage(
        EventId = 8007,
        Level = LogLevel.Debug,
        Message = "PII pipeline masking skipped for {ResponseType}: {Reason}")]
    internal static partial void PipelineMaskingSkipped(
        ILogger logger, string responseType, string reason);

    [LoggerMessage(
        EventId = 8008,
        Level = LogLevel.Warning,
        Message = "PII pipeline masking failed for response type {ResponseType}")]
    internal static partial void PipelineMaskingFailed(
        ILogger logger, Exception exception, string responseType);
}
