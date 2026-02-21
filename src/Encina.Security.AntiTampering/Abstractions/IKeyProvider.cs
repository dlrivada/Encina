using LanguageExt;

namespace Encina.Security.AntiTampering.Abstractions;

/// <summary>
/// Provides HMAC key management operations for request signing.
/// </summary>
/// <remarks>
/// <para>
/// Implementations supply the secret key material used for HMAC computation during
/// request signing and verification. Keys are identified by a string key ID, supporting
/// key rotation scenarios where multiple keys coexist.
/// </para>
/// <para>
/// This interface follows the same pattern as <c>Encina.Secrets.ISecretProvider</c>,
/// enabling implementations backed by cloud secret managers (Azure Key Vault, AWS Secrets
/// Manager, HashiCorp Vault) or simple in-memory stores for testing.
/// </para>
/// <para>
/// All methods follow the Railway Oriented Programming (ROP) pattern, returning
/// <see cref="Either{EncinaError, T}"/> to represent success or failure without exceptions.
/// </para>
/// <para>
/// Implementations must be thread-safe and suitable for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Retrieve a signing key
/// var result = await keyProvider.GetKeyAsync("api-key-v1", cancellationToken);
/// result.Match(
///     Right: keyBytes => { /* use key for HMAC */ },
///     Left: error => { /* key not found or retrieval failed */ });
/// </code>
/// </example>
public interface IKeyProvider
{
    /// <summary>
    /// Retrieves the HMAC key material for the specified key identifier.
    /// </summary>
    /// <param name="keyId">The unique identifier of the key to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;byte[]&gt;</c> containing the key material on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if the key is not found or retrieval fails.
    /// </returns>
    /// <remarks>
    /// Key material should be treated as sensitive data and cleared from memory
    /// when no longer needed. Implementations should consider using secure memory
    /// handling practices.
    /// </remarks>
    ValueTask<Either<EncinaError, byte[]>> GetKeyAsync(
        string keyId,
        CancellationToken cancellationToken = default);
}
