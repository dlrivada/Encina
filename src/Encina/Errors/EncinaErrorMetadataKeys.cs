namespace Encina;

/// <summary>
/// Well-known keys of the details dictionary of an <see cref="EncinaError"/> that protocol hosts read to write
/// response headers (ADR-062).
/// </summary>
/// <example>
/// <code>
/// var details = new Dictionary&lt;string, object?&gt;
/// {
///     [EncinaErrorMetadataKeys.RetryAfter] = TimeSpan.FromSeconds(30),
/// };
/// return EncinaErrors.Create(code, ErrorCategory.Unavailable, "The payment service is unavailable.", details: details);
/// </code>
/// </example>
public static class EncinaErrorMetadataKeys
{
    /// <summary>
    /// The time after which the caller may retry, as a <see cref="TimeSpan"/>. A host writes <c>Retry-After</c>
    /// only when the status is 429 or 503 and this key is present; it is never guessed.
    /// </summary>
    public const string RetryAfter = "encina.retry_after";

    /// <summary>
    /// The authentication challenge of a 401, as a <see cref="string"/> (for example <c>Bearer</c> or an HMAC
    /// scheme name). Every 401 carries <c>WWW-Authenticate</c>: this value when present, otherwise <c>Bearer</c>.
    /// </summary>
    public const string AuthenticateChallenge = "encina.authenticate_challenge";
}
