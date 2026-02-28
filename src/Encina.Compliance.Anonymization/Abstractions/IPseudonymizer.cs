using Encina.Compliance.Anonymization.Model;

using LanguageExt;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Service for applying reversible pseudonymization to data objects and individual values.
/// </summary>
/// <remarks>
/// <para>
/// Pseudonymization replaces identifying fields with artificial identifiers (pseudonyms),
/// making it impossible to attribute data to a specific subject without additional information
/// held separately (GDPR Article 4(5)). Unlike anonymization, pseudonymized data remains
/// personal data under GDPR but benefits from reduced regulatory burden.
/// </para>
/// <para>
/// Two algorithms are supported:
/// <list type="bullet">
/// <item>
/// <term>AES-256-GCM</term>
/// <description>Authenticated encryption providing confidentiality, integrity, and authenticity.
/// Suitable for reversible pseudonymization where the original value must be recoverable
/// (depseudonymization). Non-deterministic — the same input produces different outputs.</description>
/// </item>
/// <item>
/// <term>HMAC-SHA256</term>
/// <description>Deterministic keyed hash providing consistent pseudonyms for the same input.
/// Suitable when pseudonymized data must be searchable or joinable across datasets.
/// One-way only — depseudonymization is not possible without a lookup table.</description>
/// </item>
/// </list>
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Key management is delegated to <see cref="IKeyProvider"/>. The <c>keyId</c>
/// parameter identifies which key to use for each operation, enabling key rotation
/// without data migration.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Pseudonymize an entire object (fields decorated with [Pseudonymize])
/// var result = await pseudonymizer.PseudonymizeAsync(customer, "key-2025-01", cancellationToken);
///
/// // Pseudonymize a single value
/// var pseudonym = await pseudonymizer.PseudonymizeValueAsync(
///     "john@example.com", "key-2025-01", PseudonymizationAlgorithm.Aes256Gcm, cancellationToken);
///
/// // Reverse the pseudonymization
/// var original = await pseudonymizer.DepseudonymizeValueAsync(
///     pseudonym, "key-2025-01", cancellationToken);
/// </code>
/// </example>
public interface IPseudonymizer
{
    /// <summary>
    /// Pseudonymizes the fields of a data object using the specified cryptographic key.
    /// </summary>
    /// <typeparam name="T">The type of the data object to pseudonymize.</typeparam>
    /// <param name="data">The data object whose fields will be pseudonymized.</param>
    /// <param name="keyId">Identifier of the cryptographic key to use for pseudonymization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A new instance of <typeparamref name="T"/> with pseudonymized field values,
    /// or an <see cref="EncinaError"/> if the key was not found or pseudonymization failed.
    /// </returns>
    /// <remarks>
    /// The returned object is a modified copy — the original <paramref name="data"/> is not mutated.
    /// Fields not decorated with <c>[Pseudonymize]</c> are left unchanged.
    /// </remarks>
    ValueTask<Either<EncinaError, T>> PseudonymizeAsync<T>(
        T data,
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses pseudonymization on the fields of a data object, restoring original values.
    /// </summary>
    /// <typeparam name="T">The type of the data object to depseudonymize.</typeparam>
    /// <param name="data">The data object whose fields will be depseudonymized.</param>
    /// <param name="keyId">Identifier of the cryptographic key used during pseudonymization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A new instance of <typeparamref name="T"/> with original field values restored,
    /// or an <see cref="EncinaError"/> if the key was not found or decryption failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Depseudonymization is only possible for fields that were pseudonymized with
    /// <see cref="PseudonymizationAlgorithm.Aes256Gcm"/>. Fields pseudonymized with
    /// <see cref="PseudonymizationAlgorithm.HmacSha256"/> are one-way and cannot be reversed.
    /// </para>
    /// <para>
    /// The key identified by <paramref name="keyId"/> must be the same key used during
    /// the original pseudonymization operation.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, T>> DepseudonymizeAsync<T>(
        T data,
        string keyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Pseudonymizes a single string value using the specified algorithm and key.
    /// </summary>
    /// <param name="value">The sensitive value to pseudonymize.</param>
    /// <param name="keyId">Identifier of the cryptographic key to use.</param>
    /// <param name="algorithm">The pseudonymization algorithm to apply.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The pseudonymized value (Base64-encoded ciphertext for AES-256-GCM, or
    /// Base64-encoded HMAC for HMAC-SHA256), or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, string>> PseudonymizeValueAsync(
        string value,
        string keyId,
        PseudonymizationAlgorithm algorithm,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reverses pseudonymization on a single value, restoring the original string.
    /// </summary>
    /// <param name="pseudonym">The pseudonymized value to reverse.</param>
    /// <param name="keyId">Identifier of the cryptographic key used during pseudonymization.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// The original value before pseudonymization, or an <see cref="EncinaError"/>
    /// if the key was not found, the pseudonym is invalid, or decryption failed.
    /// </returns>
    /// <remarks>
    /// Only works for values pseudonymized with <see cref="PseudonymizationAlgorithm.Aes256Gcm"/>.
    /// Attempting to depseudonymize an HMAC-SHA256 pseudonym returns an error.
    /// </remarks>
    ValueTask<Either<EncinaError, string>> DepseudonymizeValueAsync(
        string pseudonym,
        string keyId,
        CancellationToken cancellationToken = default);
}
