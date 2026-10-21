namespace Encina.Recoverability;

/// <summary>
/// Error codes of the outbound taxonomy: the failure families of a call to an external HTTP API.
/// </summary>
/// <remarks>
/// <para>
/// There is one code per failure family; the exact HTTP status is in the
/// <see cref="OutboundErrorMetadataKeys.HttpStatus"/> metadata, so a per-status code would only duplicate
/// it. Whether the failure is worth retrying is in the <see cref="OutboundErrorMetadataKeys.Transient"/>
/// metadata, which <see cref="DefaultErrorClassifier"/> reads first.
/// </para>
/// <para>
/// A caller cancellation reuses <see cref="EncinaErrorCodes.RequestCancelled"/> instead of a code of its own.
/// The codes live in core because every outbound client (<c>Encina.Refit</c> today) reports the same taxonomy.
/// </para>
/// </remarks>
public static class OutboundErrorCodes
{
    /// <summary>
    /// The API answered with a status that a retry cannot change (4xx other than 408 and 429, 1xx, 3xx).
    /// </summary>
    public const string ClientError = "encina.outbound.client_error";

    /// <summary>
    /// The API answered with a server failure (5xx). Transient.
    /// </summary>
    public const string ServerError = "encina.outbound.server_error";

    /// <summary>
    /// The API asked the caller to slow down (429), usually with a <c>Retry-After</c> header. Transient.
    /// </summary>
    public const string RateLimited = "encina.outbound.rate_limited";

    /// <summary>
    /// The call timed out without the caller cancelling it (also a 408 response). Transient.
    /// </summary>
    public const string Timeout = "encina.outbound.timeout";

    /// <summary>
    /// The request never produced a response: DNS, connection, TLS or a broken connection. Transient.
    /// </summary>
    public const string NetworkError = "encina.outbound.network_error";

    /// <summary>
    /// A response arrived but could not be read into the expected shape (deserialization failure). Permanent.
    /// </summary>
    public const string UnexpectedResponse = "encina.outbound.unexpected_response";

    /// <summary>
    /// Any other exception while calling the API. Permanent unless it is a timeout or an I/O failure.
    /// </summary>
    public const string Unexpected = "encina.outbound.unexpected";
}
