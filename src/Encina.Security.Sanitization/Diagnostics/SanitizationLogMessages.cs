using Microsoft.Extensions.Logging;

namespace Encina.Security.Sanitization.Diagnostics;

/// <summary>
/// High-performance structured log messages for the sanitization pipeline using
/// <see cref="LoggerMessageAttribute"/>-generated methods.
/// </summary>
/// <remarks>
/// Uses compile-time source generation for zero-allocation logging when the
/// log level is not enabled. Each method corresponds to a specific pipeline event.
/// </remarks>
internal static partial class SanitizationLogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Debug,
        Message = "Starting input sanitization for {RequestType} ({PropertyCount} properties)")]
    internal static partial void InputSanitizationStarted(
        ILogger logger, string requestType, int propertyCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Debug,
        Message = "Input sanitization completed for {RequestType} in {ElapsedMs:F2}ms")]
    internal static partial void InputSanitizationCompleted(
        ILogger logger, string requestType, double elapsedMs);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "Input sanitization failed for property '{PropertyName}' on {RequestType}: {ErrorMessage}")]
    internal static partial void InputSanitizationPropertyFailed(
        ILogger logger, string propertyName, string requestType, string errorMessage);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Debug,
        Message = "Starting output encoding for {ResponseType} ({PropertyCount} properties)")]
    internal static partial void OutputEncodingStarted(
        ILogger logger, string responseType, int propertyCount);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Output encoding completed for {ResponseType} in {ElapsedMs:F2}ms")]
    internal static partial void OutputEncodingCompleted(
        ILogger logger, string responseType, double elapsedMs);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Output encoding failed for property '{PropertyName}' on {ResponseType}: {ErrorMessage}")]
    internal static partial void OutputEncodingPropertyFailed(
        ILogger logger, string propertyName, string responseType, string errorMessage);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "Skipping input sanitization for {RequestType}: no sanitizable properties found")]
    internal static partial void InputSanitizationSkipped(
        ILogger logger, string requestType);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Debug,
        Message = "Skipping output encoding for {ResponseType}: no encodable properties found")]
    internal static partial void OutputEncodingSkipped(
        ILogger logger, string responseType);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Debug,
        Message = "Auto-sanitizing all string properties for {RequestType} ({PropertyCount} properties)")]
    internal static partial void AutoSanitizationStarted(
        ILogger logger, string requestType, int propertyCount);
}
