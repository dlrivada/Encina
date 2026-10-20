namespace Encina.Diagnostics;

/// <summary>
/// The only values that may be written to an <see cref="System.Diagnostics.Activity"/> status
/// description or tag when an operation fails: the error code of an <see cref="EncinaError"/> or
/// the type name of an exception, never their messages.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EncinaError.Message"/> and <see cref="Exception.Message"/> can carry personal data
/// (a key value, a SQL fragment, a connection string), and traces leave the process to the
/// configured exporter. Every activity source and instrumented decorator takes its failure
/// description from this class so the rule lives in one place (AGENTS.md section 3, #2134).
/// </para>
/// </remarks>
public static class ActivityStatusDescriptions
{
    /// <summary>
    /// Gets the status description for a failed <see cref="EncinaError"/>: its error code, or
    /// <c>Encina.unknown</c> when the error carries none. Never the error message.
    /// </summary>
    /// <param name="error">The failure.</param>
    /// <returns>The error code.</returns>
    public static string ForError(EncinaError error) => error.GetEncinaCode();

    /// <summary>
    /// Gets the status description for an exception: its type name. Never the exception message.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>The simple name of the exception type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <c>null</c>.</exception>
    public static string ForException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception.GetType().Name;
    }
}
