using Encina.Security.AntiTampering.HMAC;
using LanguageExt;

namespace Encina.Security.AntiTampering.Abstractions;

/// <summary>
/// Provides HMAC-based request signing and verification operations.
/// </summary>
/// <remarks>
/// <para>
/// Implementations handle the cryptographic operations for signing outgoing requests
/// and verifying incoming request integrity. The default implementation uses HMAC-SHA256
/// with a canonical string representation of the request components.
/// </para>
/// <para>
/// The signing process:
/// <list type="number">
/// <item><description>Builds a <see cref="SignatureComponents"/> record from the request.</description></item>
/// <item><description>Produces a canonical string: <c>"Method|Path|PayloadHash|Timestamp|Nonce"</c>.</description></item>
/// <item><description>Computes HMAC over the canonical string using the secret key.</description></item>
/// </list>
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for use in concurrent pipeline execution.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Signing an outgoing request
/// var context = new SigningContext
/// {
///     KeyId = "api-key-v1",
///     Nonce = Guid.NewGuid().ToString("N"),
///     Timestamp = DateTimeOffset.UtcNow,
///     HttpMethod = "POST",
///     RequestPath = "/api/orders"
/// };
/// var result = await signer.SignAsync(payload, context, cancellationToken);
///
/// // Verifying an incoming request
/// var verifyResult = await signer.VerifyAsync(payload, signature, context, cancellationToken);
/// </code>
/// </example>
public interface IRequestSigner
{
    /// <summary>
    /// Signs a request payload using the specified signing context.
    /// </summary>
    /// <param name="payload">The serialized request payload to sign.</param>
    /// <param name="context">The signing context containing key ID, nonce, timestamp, and request metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;string&gt;</c> containing the Base64-encoded HMAC signature on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if signing fails (e.g., key not found, algorithm not supported).
    /// </returns>
    ValueTask<Either<EncinaError, string>> SignAsync(
        ReadOnlyMemory<byte> payload,
        SigningContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a request payload signature against the expected HMAC value.
    /// </summary>
    /// <param name="payload">The serialized request payload to verify.</param>
    /// <param name="signature">The Base64-encoded HMAC signature to verify against.</param>
    /// <param name="context">The signing context containing key ID, nonce, timestamp, and request metadata.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;bool&gt;</c> with <c>true</c> if the signature is valid, <c>false</c> if invalid, or
    /// <c>Left&lt;EncinaError&gt;</c> if verification fails (e.g., key not found).
    /// </returns>
    /// <remarks>
    /// Implementations must use constant-time comparison to prevent timing attacks.
    /// </remarks>
    ValueTask<Either<EncinaError, bool>> VerifyAsync(
        ReadOnlyMemory<byte> payload,
        string signature,
        SigningContext context,
        CancellationToken cancellationToken = default);
}
