using System.Text;

namespace Encina.Security.AntiTampering.HMAC;

/// <summary>
/// Represents the individual components that form the canonical string for HMAC signature computation.
/// </summary>
/// <remarks>
/// <para>
/// The canonical string is produced by <see cref="ToCanonicalString"/> in the format:
/// <c>"Method|Path|PayloadHash|Timestamp|Nonce"</c>.
/// </para>
/// <para>
/// This deterministic format ensures that both the signer and verifier produce identical
/// canonical representations, allowing signature comparison to succeed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var components = new SignatureComponents
/// {
///     HttpMethod = "POST",
///     RequestPath = "/api/orders",
///     PayloadHash = "abc123def456...",
///     Timestamp = "2026-02-21T12:00:00Z",
///     Nonce = "550e8400-e29b-41d4-a716-446655440000",
///     KeyId = "api-key-v1"
/// };
///
/// string canonical = components.ToCanonicalString();
/// // "POST|/api/orders|abc123def456...|2026-02-21T12:00:00Z|550e8400-e29b-41d4-a716-446655440000"
/// </code>
/// </example>
public sealed record SignatureComponents
{
    /// <summary>
    /// Gets the HTTP method of the request.
    /// </summary>
    /// <remarks>
    /// Uppercase HTTP verb (e.g., <c>POST</c>, <c>GET</c>). Always normalized to uppercase
    /// to ensure canonical consistency.
    /// </remarks>
    public required string HttpMethod { get; init; }

    /// <summary>
    /// Gets the request path.
    /// </summary>
    /// <remarks>
    /// The URL path of the request (e.g., <c>/api/orders</c>). Does not include
    /// query string parameters.
    /// </remarks>
    public required string RequestPath { get; init; }

    /// <summary>
    /// Gets the SHA-256 hash of the request payload.
    /// </summary>
    /// <remarks>
    /// Hex-encoded lowercase SHA-256 digest of the request body. For requests with
    /// no body, this should be the hash of an empty byte array.
    /// </remarks>
    public required string PayloadHash { get; init; }

    /// <summary>
    /// Gets the ISO 8601 formatted timestamp of the request.
    /// </summary>
    /// <remarks>
    /// The timestamp in round-trip ("O") format (e.g., <c>"2026-02-21T12:00:00.0000000+00:00"</c>).
    /// Used for timestamp tolerance validation.
    /// </remarks>
    public required string Timestamp { get; init; }

    /// <summary>
    /// Gets the unique nonce for replay protection.
    /// </summary>
    /// <remarks>
    /// A unique value per request, typically a UUID or random string.
    /// </remarks>
    public required string Nonce { get; init; }

    /// <summary>
    /// Gets the identifier of the HMAC key used for signing.
    /// </summary>
    /// <remarks>
    /// The key ID is not included in the canonical string but is needed to retrieve
    /// the correct secret key for HMAC computation.
    /// </remarks>
    public required string KeyId { get; init; }

    /// <summary>
    /// Produces the canonical string representation used for HMAC computation.
    /// </summary>
    /// <returns>
    /// A pipe-delimited string in the format: <c>"Method|Path|PayloadHash|Timestamp|Nonce"</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The canonical string is deterministic: given the same input components, it always
    /// produces the same output. This is critical for signature verification.
    /// </para>
    /// <para>
    /// The <see cref="KeyId"/> is intentionally excluded from the canonical string. The key ID
    /// is used to look up the secret, not as part of the signed content.
    /// </para>
    /// </remarks>
    public string ToCanonicalString()
    {
        return new StringBuilder(256)
            .Append(HttpMethod)
            .Append('|')
            .Append(RequestPath)
            .Append('|')
            .Append(PayloadHash)
            .Append('|')
            .Append(Timestamp)
            .Append('|')
            .Append(Nonce)
            .ToString();
    }
}
