namespace Encina.Recoverability;

/// <summary>
/// Keys of the metadata that an outbound error carries (<see cref="EncinaErrorExtensions.GetMetadata(EncinaError)"/>).
/// </summary>
/// <remarks>
/// The metadata never contains a response body, an error message or a URL; it holds only the values below.
/// </remarks>
public static class OutboundErrorMetadataKeys
{
    /// <summary>
    /// <c>http.status</c> (<see cref="int"/>): the HTTP status of the response. Present only when a response arrived.
    /// </summary>
    public const string HttpStatus = "http.status";

    /// <summary>
    /// <c>http.retry_after</c> (<see cref="TimeSpan"/>): how long the API asked the caller to wait. Present only
    /// when a valid <c>Retry-After</c> header arrived.
    /// </summary>
    public const string HttpRetryAfter = "http.retry_after";

    /// <summary>
    /// <c>transient</c> (<see cref="bool"/>): whether a retry may succeed. Present on every outbound error.
    /// </summary>
    public const string Transient = "transient";

    /// <summary>
    /// <c>outbound.ambiguous</c> (<see cref="bool"/>): the failure is transient but the request may already have
    /// been processed (a 500, 502 or 504, a timeout, or a broken connection after the request was sent) and the
    /// method was POST or PATCH. Present only when <see langword="true"/>. See
    /// <see cref="OutboundRetryRules.AllowsAutomaticRetry(EncinaError, string?, bool)"/>.
    /// </summary>
    public const string Ambiguous = "outbound.ambiguous";
}
