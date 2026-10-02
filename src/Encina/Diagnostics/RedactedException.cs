using System.Text;

namespace Encina.Diagnostics;

/// <summary>
/// A copy of an exception that is safe to hand to a logger: it keeps the exception type and the
/// full stack trace (inner exceptions included) but never the original message.
/// </summary>
/// <remarks>
/// <para>
/// Exception messages can carry personal data (a data-subject id, a connection string, a payload
/// fragment), and logger providers print <see cref="Exception.Message"/> and
/// <see cref="Exception.ToString"/> verbatim. AGENTS.md section 3 therefore forbids messages in
/// logs; pass <c>ex.ForLogging()</c> instead of <c>ex</c> to a logger.
/// </para>
/// <para>
/// <see cref="Exception.Message"/> is the full name of the original exception type,
/// <see cref="StackTrace"/> is the original stack trace, <see cref="Exception.InnerException"/>
/// and <see cref="InnerExceptions"/> hold the redacted inner exceptions. The original
/// <see cref="Exception.Data"/> dictionary is not copied.
/// </para>
/// <para>
/// Sinks that group by exception type (for example the OpenTelemetry <c>exception.type</c> attribute)
/// see <see cref="RedactedException"/> for every failure; the original type name is in
/// <see cref="Exception.Message"/> and in the first line of <see cref="ToString"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// catch (Exception ex)
/// {
///     Log.StepFailed(_logger, sagaId, ex.GetType().Name, ex.ForLogging());
/// }
/// </code>
/// </example>
public sealed class RedactedException : Exception
{
    private readonly string? _stackTrace;

    private RedactedException(string typeName, string? stackTrace, IReadOnlyList<Exception> innerExceptions)
        : base(typeName, innerExceptions.Count > 0 ? innerExceptions[0] : null)
    {
        _stackTrace = stackTrace;
        InnerExceptions = innerExceptions;
    }

    /// <summary>
    /// Gets the redacted inner exceptions: the single inner exception, or every inner exception
    /// of an <see cref="AggregateException"/>.
    /// </summary>
    public IReadOnlyList<Exception> InnerExceptions { get; }

    /// <summary>
    /// Gets the stack trace of the original exception.
    /// </summary>
    public override string? StackTrace => _stackTrace;

    /// <summary>
    /// Creates the redacted copy of an exception, recursively for its inner exceptions.
    /// </summary>
    /// <param name="exception">The exception to redact.</param>
    /// <returns>
    /// A <see cref="RedactedException"/> with the type, stack trace and redacted inners. It never
    /// throws for a non-null exception: when reading the stack trace or the inner exceptions fails,
    /// the result carries only the type full name.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
    public static RedactedException From(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is RedactedException alreadyRedacted)
        {
            return alreadyRedacted;
        }

        var type = exception.GetType();
        var typeName = type.FullName ?? type.Name;

        try
        {
            var inners = GetInnerExceptions(exception).Select(From).ToArray<Exception>();

            return new RedactedException(typeName, exception.StackTrace, inners);
        }
#pragma warning disable CA1031 // Redaction runs on failure paths and must never throw: any failure falls back to the bare type name.
        catch (Exception)
#pragma warning restore CA1031
        {
            return new RedactedException(typeName, null, []);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var text = new StringBuilder(Message);

        foreach (var inner in InnerExceptions)
        {
            text.AppendLine().Append(" ---> ").Append(inner);
        }

        if (_stackTrace is not null)
        {
            text.AppendLine().Append(_stackTrace);
        }

        return text.ToString();
    }

    private static Exception[] GetInnerExceptions(Exception exception)
        => exception is AggregateException aggregate
            ? [.. aggregate.InnerExceptions]
            : exception.InnerException is { } inner ? [inner] : [];
}

/// <summary>
/// Extension methods to log exceptions without their messages.
/// </summary>
public static class ExceptionLoggingExtensions
{
    /// <summary>
    /// Returns a copy of the exception that keeps its type and stack trace but not its message,
    /// to be passed to a logger (see <see cref="RedactedException"/>).
    /// </summary>
    /// <param name="exception">The exception to redact.</param>
    /// <returns>The redacted exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
    public static Exception ForLogging(this Exception exception) => RedactedException.From(exception);
}
