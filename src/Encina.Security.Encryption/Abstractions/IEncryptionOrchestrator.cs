using LanguageExt;

namespace Encina.Security.Encryption.Abstractions;

/// <summary>
/// Coordinates encryption and decryption of multiple properties on a request or response object.
/// </summary>
/// <remarks>
/// <para>
/// The orchestrator discovers properties marked with encryption attributes and applies
/// the appropriate encryption or decryption operations. It follows the same pattern as
/// <c>ValidationOrchestrator</c>, acting as the coordination
/// layer between attribute discovery, key management, and cryptographic operations.
/// </para>
/// <para>
/// The orchestrator handles:
/// <list type="bullet">
/// <item>Discovering properties decorated with encryption attributes</item>
/// <item>Building the appropriate <see cref="EncryptionContext"/> per property</item>
/// <item>Coordinating with <see cref="IFieldEncryptor"/> for cryptographic operations</item>
/// <item>Aggregating results and errors across multiple properties</item>
/// </list>
/// </para>
/// </remarks>
public interface IEncryptionOrchestrator
{
    /// <summary>
    /// Encrypts all properties marked for encryption on the given object.
    /// </summary>
    /// <typeparam name="T">The type of object containing properties to encrypt.</typeparam>
    /// <param name="instance">The object whose marked properties should be encrypted.</param>
    /// <param name="context">The request context providing tenant and user information.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;T&gt;</c> with encrypted property values on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if any encryption operation fails.
    /// </returns>
    /// <remarks>
    /// Properties without encryption attributes are left unchanged.
    /// If any property encryption fails, the entire operation returns an error.
    /// </remarks>
    ValueTask<Either<EncinaError, T>> EncryptAsync<T>(
        T instance,
        IRequestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypts all encrypted properties on the given object.
    /// </summary>
    /// <typeparam name="T">The type of object containing properties to decrypt.</typeparam>
    /// <param name="instance">The object whose marked properties should be decrypted.</param>
    /// <param name="context">The request context providing tenant and user information.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right&lt;T&gt;</c> with decrypted property values on success, or
    /// <c>Left&lt;EncinaError&gt;</c> if any decryption operation fails.
    /// </returns>
    /// <remarks>
    /// Properties without encrypted values are left unchanged.
    /// If any property decryption fails, the entire operation returns an error.
    /// </remarks>
    ValueTask<Either<EncinaError, T>> DecryptAsync<T>(
        T instance,
        IRequestContext context,
        CancellationToken cancellationToken = default);
}
