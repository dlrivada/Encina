using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Store for managing persistent token-to-value mappings used by <see cref="ITokenizer"/>.
/// </summary>
/// <remarks>
/// <para>
/// The token mapping store provides CRUD operations for <see cref="TokenMapping"/> records,
/// enabling reversible tokenization (detokenization). Each mapping associates a token with
/// the encrypted original value, keyed by an HMAC hash for deduplication.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store mappings in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// <para>
/// When a cryptographic key is rotated, all token mappings referencing the old key must be
/// re-encrypted with the new key. The <see cref="DeleteByKeyIdAsync"/> method supports
/// cleanup of mappings associated with retired keys.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Store a new token mapping
/// var mapping = TokenMapping.Create(
///     token: "tok_a1b2c3d4-e5f6-7890-abcd-ef1234567890",
///     originalValueHash: "hmac_sha256_base64_hash",
///     encryptedOriginalValue: encryptedBytes,
///     keyId: "key-2025-01");
///
/// await store.StoreAsync(mapping, cancellationToken);
///
/// // Look up a mapping by token
/// var result = await store.GetByTokenAsync("tok_a1b2c3d4-e5f6-7890-abcd-ef1234567890", cancellationToken);
/// </code>
/// </example>
public interface ITokenMappingStore
{
    /// <summary>
    /// Persists a new token mapping record.
    /// </summary>
    /// <param name="mapping">The token mapping to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the mapping
    /// could not be stored (e.g., duplicate token).
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> StoreAsync(
        TokenMapping mapping,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token mapping by its token value.
    /// </summary>
    /// <param name="token">The token to look up.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(mapping)</c> if a mapping with the given token exists,
    /// <c>None</c> if no mapping is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used during detokenization to retrieve the encrypted original value
    /// associated with a token.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a token mapping by the hash of the original value.
    /// </summary>
    /// <param name="hash">The HMAC-SHA256 hash of the original value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <c>Some(mapping)</c> if a mapping with the given hash exists,
    /// <c>None</c> if no mapping is found, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used during tokenization for deduplication — if the same original value has
    /// already been tokenized, the existing token is returned instead of creating
    /// a duplicate mapping.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<TokenMapping>>> GetByOriginalValueHashAsync(
        string hash,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all token mappings associated with a specific cryptographic key.
    /// </summary>
    /// <param name="keyId">The identifier of the key whose mappings should be deleted.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Used during key retirement to clean up mappings that can no longer be decrypted.
    /// Typically called after all mappings have been re-encrypted with a new key
    /// during key rotation.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeleteByKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all token mappings in the store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all token mappings, or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// Primarily used for key rotation workflows (re-encrypt all mappings with new key)
    /// and compliance auditing. For large datasets, consider implementing pagination
    /// in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<TokenMapping>>> GetAllAsync(
        CancellationToken cancellationToken = default);
}
