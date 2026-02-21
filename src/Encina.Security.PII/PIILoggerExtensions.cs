using Encina.Security.PII.Abstractions;
using Microsoft.Extensions.Logging;

namespace Encina.Security.PII;

/// <summary>
/// Extension methods for <see cref="ILogger"/> that mask PII before logging.
/// </summary>
/// <remarks>
/// <para>
/// These methods ensure sensitive data is redacted prior to being written to
/// any log sink. Each method checks <see cref="ILogger.IsEnabled(LogLevel)"/>
/// before performing any masking work, so disabled log levels incur no overhead.
/// </para>
/// <para>
/// All methods accept an <see cref="IPIIMasker"/> instance explicitly.
/// Inject <see cref="IPIIMasker"/> in the calling class via constructor injection
/// and pass it to these extensions.
/// </para>
/// <para>
/// If masking fails for any reason, a <c>"[MASKING FAILED]"</c> placeholder is logged
/// instead of the raw data, ensuring sensitive information is never written to logs
/// even on failure.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class UserService
/// {
///     private readonly ILogger&lt;UserService&gt; _logger;
///     private readonly IPIIMasker _masker;
///
///     public UserService(ILogger&lt;UserService&gt; logger, IPIIMasker masker)
///     {
///         _logger = logger;
///         _masker = masker;
///     }
///
///     public void ProcessUser(UserDto user)
///     {
///         _logger.LogInformationMasked(_masker, "Processing user: {@User}", user);
///     }
/// }
/// </code>
/// </example>
public static class PIILoggerExtensions
{
    /// <summary>
    /// Logs a message at the specified <paramref name="level"/> with PII-masked data.
    /// </summary>
    /// <typeparam name="T">The type of the data to mask. Must be a reference type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="masker">The PII masker to apply before logging.</param>
    /// <param name="level">The log level.</param>
    /// <param name="message">
    /// The message template with a single placeholder for the masked data
    /// (e.g., <c>"Processing: {@Data}"</c>).
    /// </param>
    /// <param name="data">The object containing PII to mask before logging.</param>
    /// <remarks>
    /// The method short-circuits when <paramref name="level"/> is not enabled on the logger,
    /// avoiding masking and serialization overhead for suppressed log levels.
    /// </remarks>
    public static void LogMasked<T>(
        this ILogger logger,
        IPIIMasker masker,
        LogLevel level,
        string message,
        T data)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(masker);

        if (!logger.IsEnabled(level))
        {
            return;
        }

        try
        {
            var masked = masker.MaskObject(data);

#pragma warning disable CA2254 // Template should be a static expression — intentional: this extension forwards the caller's template
            logger.Log(level, message, masked);
#pragma warning restore CA2254
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            // Never leak raw PII — log a safe fallback
#pragma warning disable CA2254 // Template should be a static expression — intentional: this extension forwards the caller's template
            logger.Log(level, message, "[MASKING FAILED]");
#pragma warning restore CA2254
            logger.LogWarning(ex, "PII masking failed during logging for type {TypeName}", typeof(T).Name);
        }
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Information"/> with PII-masked data.
    /// </summary>
    /// <typeparam name="T">The type of the data to mask. Must be a reference type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="masker">The PII masker to apply before logging.</param>
    /// <param name="message">
    /// The message template with a single placeholder for the masked data.
    /// </param>
    /// <param name="data">The object containing PII to mask before logging.</param>
    /// <example>
    /// <code>
    /// _logger.LogInformationMasked(_masker, "User registered: {@User}", userDto);
    /// </code>
    /// </example>
    public static void LogInformationMasked<T>(
        this ILogger logger,
        IPIIMasker masker,
        string message,
        T data)
        where T : class
    {
        LogMasked(logger, masker, LogLevel.Information, message, data);
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Warning"/> with PII-masked data.
    /// </summary>
    /// <typeparam name="T">The type of the data to mask. Must be a reference type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="masker">The PII masker to apply before logging.</param>
    /// <param name="message">
    /// The message template with a single placeholder for the masked data.
    /// </param>
    /// <param name="data">The object containing PII to mask before logging.</param>
    /// <example>
    /// <code>
    /// _logger.LogWarningMasked(_masker, "Suspicious activity for: {@User}", userDto);
    /// </code>
    /// </example>
    public static void LogWarningMasked<T>(
        this ILogger logger,
        IPIIMasker masker,
        string message,
        T data)
        where T : class
    {
        LogMasked(logger, masker, LogLevel.Warning, message, data);
    }

    /// <summary>
    /// Logs a message at <see cref="LogLevel.Error"/> with PII-masked data.
    /// </summary>
    /// <typeparam name="T">The type of the data to mask. Must be a reference type.</typeparam>
    /// <param name="logger">The logger instance.</param>
    /// <param name="masker">The PII masker to apply before logging.</param>
    /// <param name="message">
    /// The message template with a single placeholder for the masked data.
    /// </param>
    /// <param name="data">The object containing PII to mask before logging.</param>
    /// <example>
    /// <code>
    /// _logger.LogErrorMasked(_masker, "Failed to process payment for: {@Order}", orderDto);
    /// </code>
    /// </example>
    public static void LogErrorMasked<T>(
        this ILogger logger,
        IPIIMasker masker,
        string message,
        T data)
        where T : class
    {
        LogMasked(logger, masker, LogLevel.Error, message, data);
    }
}
