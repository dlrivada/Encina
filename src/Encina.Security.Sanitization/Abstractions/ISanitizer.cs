namespace Encina.Security.Sanitization.Abstractions;

/// <summary>
/// Provides input sanitization operations for different output contexts.
/// </summary>
/// <remarks>
/// <para>
/// Sanitization transforms input by removing or escaping dangerous content while preserving
/// safe content. Unlike validation (which rejects input), sanitization cleans input so it can
/// be safely used in the target context.
/// </para>
/// <para>
/// Each method targets a specific output context (HTML, SQL, Shell, JSON, XML) and applies
/// context-appropriate sanitization rules. Use <see cref="Custom"/> with a
/// <see cref="ISanitizationProfile"/> for fine-grained control over allowed content.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for use in concurrent pipeline execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Sanitize user-provided HTML content
/// string safeHtml = sanitizer.SanitizeHtml("&lt;script&gt;alert('xss')&lt;/script&gt;&lt;p&gt;Hello&lt;/p&gt;");
/// // Result: "&lt;p&gt;Hello&lt;/p&gt;"
///
/// // Custom profile for rich text editing
/// string richText = sanitizer.Custom(userInput, SanitizationProfiles.RichText);
/// </code>
/// </example>
public interface ISanitizer
{
    /// <summary>
    /// Sanitizes HTML input by removing dangerous elements and attributes.
    /// </summary>
    /// <param name="input">The HTML string to sanitize.</param>
    /// <returns>The sanitized HTML string with dangerous content removed.</returns>
    /// <remarks>
    /// Uses the default sanitization profile which strips all scripts, event handlers,
    /// and potentially dangerous HTML elements. Safe formatting tags are preserved.
    /// </remarks>
    string SanitizeHtml(string input);

    /// <summary>
    /// Sanitizes input for safe use in SQL contexts.
    /// </summary>
    /// <param name="input">The string to sanitize for SQL context.</param>
    /// <returns>The sanitized string safe for SQL interpolation.</returns>
    /// <remarks>
    /// <para>
    /// <b>Important:</b> Parameterized queries are always preferred over sanitization for SQL.
    /// This method provides defense-in-depth for scenarios where parameterization is not possible
    /// (e.g., dynamic column names, ORDER BY clauses).
    /// </para>
    /// <para>
    /// Escapes single quotes, removes SQL comment sequences, and neutralizes common
    /// SQL injection patterns.
    /// </para>
    /// </remarks>
    string SanitizeForSql(string input);

    /// <summary>
    /// Sanitizes input for safe use in shell/command-line contexts.
    /// </summary>
    /// <param name="input">The string to sanitize for shell context.</param>
    /// <returns>The sanitized string safe for shell interpolation.</returns>
    /// <remarks>
    /// Escapes or removes shell metacharacters including pipes, semicolons, backticks,
    /// and other command injection vectors.
    /// </remarks>
    string SanitizeForShell(string input);

    /// <summary>
    /// Sanitizes input for safe use in JSON contexts.
    /// </summary>
    /// <param name="input">The string to sanitize for JSON context.</param>
    /// <returns>The sanitized string safe for JSON embedding.</returns>
    /// <remarks>
    /// Escapes control characters, quotes, and backslashes according to JSON specification (RFC 8259).
    /// </remarks>
    string SanitizeForJson(string input);

    /// <summary>
    /// Sanitizes input for safe use in XML contexts.
    /// </summary>
    /// <param name="input">The string to sanitize for XML context.</param>
    /// <returns>The sanitized string safe for XML embedding.</returns>
    /// <remarks>
    /// Escapes XML special characters (&amp;, &lt;, &gt;, &quot;, &apos;) and removes
    /// invalid XML characters.
    /// </remarks>
    string SanitizeForXml(string input);

    /// <summary>
    /// Sanitizes input using a custom sanitization profile.
    /// </summary>
    /// <param name="input">The string to sanitize.</param>
    /// <param name="profile">The sanitization profile defining allowed content.</param>
    /// <returns>The sanitized string according to the profile rules.</returns>
    /// <remarks>
    /// Use custom profiles for fine-grained control over which HTML tags, attributes,
    /// and protocols are allowed. See <see cref="ISanitizationProfile"/> for profile configuration.
    /// </remarks>
    string Custom(string input, ISanitizationProfile profile);
}
