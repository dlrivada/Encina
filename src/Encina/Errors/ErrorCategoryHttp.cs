using System.Collections.Frozen;
using System.Globalization;

namespace Encina;

/// <summary>
/// The HTTP column of the error category contract (ADR-062): the status of each <see cref="ErrorCategory"/>, the
/// reason phrase of a status and the response headers (<c>Retry-After</c>, <c>WWW-Authenticate</c>) an error
/// carries. It is written once here so every REST host answers the same status and headers for the same category.
/// </summary>
/// <remarks>
/// <para>
/// The class reads only the category and the details keys <see cref="EncinaErrorMetadataKeys.RetryAfter"/> and
/// <see cref="EncinaErrorMetadataKeys.AuthenticateChallenge"/>; it never reads <see cref="EncinaError.Message"/>.
/// No method throws: an undefined category answers 500, an unknown status gets a generic reason phrase and an
/// unusable header value is ignored. The texts are English and invariant.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var status = error.GetStatusCode();
/// var title = ErrorCategoryHttp.GetReasonPhrase(status);
/// foreach (var header in ErrorCategoryHttp.GetResponseHeaders(error, status))
/// {
///     response.Headers[header.Key] = header.Value;
/// }
/// </code>
/// </example>
public static class ErrorCategoryHttp
{
    private const int InternalServerError = 500;

    private const string DefaultReasonPhrase = "An error occurred";

    private const string DefaultChallenge = "Bearer";

    private const string RetryAfterHeader = "Retry-After";

    private const string AuthenticateHeader = "WWW-Authenticate";

    private static readonly KeyValuePair<string, string>[] NoHeaders = [];

    // Indexed by (int)ErrorCategory; the values are explicit, stable and contiguous from 0 (ErrorCategory).
    private static readonly int[] StatusByCategory =
    [
        500, // Internal
        400, // Validation
        400, // InvalidRequest
        401, // Unauthenticated
        403, // Forbidden
        404, // NotFound
        409, // Conflict
        409, // AlreadyExists
        400, // FailedPrecondition (provisional, owned by #3420)
        429, // RateLimited
        503, // Unavailable
        504, // Timeout
        502, // UpstreamFailure
        499, // Cancelled
    ];

    private static readonly FrozenDictionary<int, string> ReasonPhrases = new Dictionary<int, string>
    {
        [400] = "Bad Request",
        [401] = "Unauthorized",
        [403] = "Forbidden",
        [404] = "Not Found",
        [409] = "Conflict",
        [422] = "Unprocessable Entity",
        [429] = "Too Many Requests",
        [499] = "Client Closed Request",
        [500] = "Internal Server Error",
        [502] = "Bad Gateway",
        [503] = "Service Unavailable",
        [504] = "Gateway Timeout",
    }.ToFrozenDictionary();

    /// <summary>
    /// Returns the HTTP status of a category (ADR-062). A value outside the enum answers 500 and never throws.
    /// </summary>
    /// <param name="category">The category.</param>
    /// <returns>The HTTP status, between 400 and 599.</returns>
    public static int GetStatusCode(ErrorCategory category)
    {
        var index = (int)category;

        return (uint)index < (uint)StatusByCategory.Length ? StatusByCategory[index] : InternalServerError;
    }

    /// <summary>
    /// Returns the HTTP status of the category of an error, read with
    /// <see cref="EncinaErrorExtensions.GetCategory(EncinaError)"/>.
    /// </summary>
    /// <param name="error">The error.</param>
    /// <returns>The HTTP status, between 400 and 599.</returns>
    public static int GetStatusCode(this EncinaError error) => GetStatusCode(error.GetCategory());

    /// <summary>
    /// Returns the reason phrase of an HTTP status, for the <c>title</c> of a problem body.
    /// </summary>
    /// <param name="statusCode">The HTTP status.</param>
    /// <returns>The reason phrase, or <c>An error occurred</c> when the status is not known.</returns>
    public static string GetReasonPhrase(int statusCode) =>
        ReasonPhrases.TryGetValue(statusCode, out var phrase) ? phrase : DefaultReasonPhrase;

    /// <summary>
    /// Returns the response headers an error carries for the status the host answers.
    /// </summary>
    /// <param name="error">The error whose details are read.</param>
    /// <param name="effectiveStatusCode">The status the host writes, which may be an explicit override of the category's status.</param>
    /// <returns>
    /// <c>Retry-After</c> (delta-seconds, rounded up) for 429 and 503 when the details hold a non-negative
    /// <see cref="TimeSpan"/> under <see cref="EncinaErrorMetadataKeys.RetryAfter"/>; <c>WWW-Authenticate</c> for 401
    /// (the details value of <see cref="EncinaErrorMetadataKeys.AuthenticateChallenge"/> when it is a non-blank
    /// string of printable ASCII (0x20-0x7E), otherwise <c>Bearer</c>); an empty list otherwise.
    /// </returns>
    public static IReadOnlyList<KeyValuePair<string, string>> GetResponseHeaders(EncinaError error, int effectiveStatusCode)
    {
        if (effectiveStatusCode == 401)
        {
            return [new(AuthenticateHeader, GetChallenge(error))];
        }

        if (effectiveStatusCode is 429 or 503)
        {
            return GetRetryAfterHeaders(error);
        }

        return NoHeaders;
    }

    private static KeyValuePair<string, string>[] GetRetryAfterHeaders(EncinaError error)
    {
        if (!error.GetDetails().TryGetValue(EncinaErrorMetadataKeys.RetryAfter, out var value)
            || value is not TimeSpan retryAfter
            || retryAfter < TimeSpan.Zero)
        {
            return NoHeaders;
        }

        var seconds = (long)Math.Ceiling(retryAfter.TotalSeconds);

        return [new(RetryAfterHeader, seconds.ToString(CultureInfo.InvariantCulture))];
    }

    private static string GetChallenge(EncinaError error)
    {
        if (error.GetDetails().TryGetValue(EncinaErrorMetadataKeys.AuthenticateChallenge, out var value)
            && value is string challenge
            && IsUsableHeaderValue(challenge))
        {
            return challenge;
        }

        return DefaultChallenge;
    }

    private static bool IsUsableHeaderValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character is < ' ' or > '~')
            {
                return false;
            }
        }

        return true;
    }
}
