using LanguageExt;

namespace Encina.Security.AntiTampering.Abstractions;

/// <summary>
/// Client for signing outgoing HTTP requests with HMAC signatures.
/// </summary>
/// <remarks>
/// <para>
/// Provides a high-level API for signing <see cref="HttpRequestMessage"/> instances
/// by reading the request body, generating cryptographic components (nonce, timestamp),
/// and attaching the HMAC signature as HTTP headers.
/// </para>
/// <para>
/// The following headers are added to the request:
/// <list type="bullet">
/// <item><description><c>X-Signature</c> — The Base64-encoded HMAC signature.</description></item>
/// <item><description><c>X-Timestamp</c> — The ISO 8601 timestamp of signing.</description></item>
/// <item><description><c>X-Nonce</c> — A unique nonce for replay protection.</description></item>
/// <item><description><c>X-Key-Id</c> — The identifier of the signing key used.</description></item>
/// </list>
/// </para>
/// <para>
/// Use with <see cref="HttpClient"/> to sign requests before sending:
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
/// {
///     Content = JsonContent.Create(new { Amount = 99.99m })
/// };
///
/// var result = await signingClient.SignRequestAsync(request, "api-key-v1", cancellationToken);
/// result.Match(
///     Right: signedRequest => httpClient.SendAsync(signedRequest),
///     Left: error => throw new InvalidOperationException(error.Message));
/// </code>
/// </example>
public interface IRequestSigningClient
{
    /// <summary>
    /// Signs an outgoing HTTP request by computing an HMAC signature and attaching it as headers.
    /// </summary>
    /// <param name="request">The HTTP request message to sign. The body content is read to compute the payload hash.</param>
    /// <param name="keyId">The identifier of the HMAC key to use for signing.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;HttpRequestMessage&gt;</c> with the signed request (headers added) on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if signing fails (e.g., key not found, body read failure).
    /// </returns>
    /// <remarks>
    /// <para>
    /// The original <paramref name="request"/> is mutated (headers added) and returned.
    /// If the request has no body, an empty payload is used for the hash computation.
    /// </para>
    /// <para>
    /// The nonce is generated as a new <see cref="Guid"/> in "N" format (32 hex chars, no dashes).
    /// The timestamp is captured at the moment of signing using the injected <see cref="TimeProvider"/>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, HttpRequestMessage>> SignRequestAsync(
        HttpRequestMessage request,
        string keyId,
        CancellationToken cancellationToken = default);
}
