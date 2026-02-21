namespace Encina.Security.AntiTampering.HMAC;

/// <summary>
/// Represents the context required to sign or verify a request.
/// </summary>
/// <remarks>
/// <para>
/// Contains the metadata needed to construct a canonical string for HMAC computation.
/// This record is used both for signing outgoing requests and verifying incoming ones.
/// </para>
/// <para>
/// For outgoing requests, the caller populates all properties. For incoming requests,
/// values are extracted from HTTP headers by the pipeline behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = new SigningContext
/// {
///     KeyId = "api-key-v1",
///     Nonce = Guid.NewGuid().ToString("N"),
///     Timestamp = DateTimeOffset.UtcNow,
///     HttpMethod = "POST",
///     RequestPath = "/api/orders"
/// };
/// </code>
/// </example>
public sealed record SigningContext
{
    /// <summary>
    /// Gets the identifier of the HMAC key used for signing.
    /// </summary>
    /// <remarks>
    /// Maps to the <c>X-Key-Id</c> HTTP header. Used by <see cref="Abstractions.IKeyProvider"/>
    /// to retrieve the corresponding secret key material.
    /// </remarks>
    public required string KeyId { get; init; }

    /// <summary>
    /// Gets the unique nonce for replay protection.
    /// </summary>
    /// <remarks>
    /// A unique value per request (typically a UUID or random string) used to prevent
    /// replay attacks. Maps to the <c>X-Nonce</c> HTTP header.
    /// </remarks>
    public required string Nonce { get; init; }

    /// <summary>
    /// Gets the timestamp of the request.
    /// </summary>
    /// <remarks>
    /// The time at which the request was signed, in UTC. Maps to the <c>X-Timestamp</c>
    /// HTTP header in ISO 8601 format. Used for timestamp tolerance validation.
    /// </remarks>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the HTTP method of the request.
    /// </summary>
    /// <remarks>
    /// The HTTP verb (e.g., <c>GET</c>, <c>POST</c>, <c>PUT</c>, <c>DELETE</c>).
    /// Included in the canonical string to prevent method-substitution attacks.
    /// </remarks>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    /// <remarks>
    /// The URL path of the request (e.g., <c>/api/orders</c>). Included in the canonical
    /// string to prevent path-substitution attacks.
    /// </remarks>
    public required string RequestPath { get; init; }
}
