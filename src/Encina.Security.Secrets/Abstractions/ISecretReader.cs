using LanguageExt;

namespace Encina.Security.Secrets.Abstractions;

/// <summary>
/// Defines the contract for reading secrets using Railway Oriented Programming.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary interface for secret access. Most consumers only need to read secrets,
/// so inject <see cref="ISecretReader"/> rather than broader interfaces.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe for concurrent access.
/// </para>
/// <para>
/// <b>Error handling:</b> All methods return <c>Either&lt;EncinaError, T&gt;</c> following
/// Encina's Railway Oriented Programming pattern. Use error codes from <see cref="SecretsErrors"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // DI-first pattern (recommended)
/// public class MyService(ISecretReader secretReader)
/// {
///     public async Task&lt;string&gt; GetApiKeyAsync(CancellationToken ct)
///     {
///         var result = await secretReader.GetSecretAsync("api-key", ct);
///         return result.Match(
///             Right: value => value,
///             Left: error => throw new InvalidOperationException(error.Message));
///     }
/// }
/// </code>
/// </example>
public interface ISecretReader
{
    /// <summary>
    /// Gets a secret value by name, returning the latest version.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(value)</c> if found;
    /// <c>Left(EncinaError)</c> with code <see cref="SecretsErrors.NotFoundCode"/> if not found,
    /// or other error codes for provider failures.
    /// </returns>
    ValueTask<Either<EncinaError, string>> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a secret and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The type to deserialize the secret value into.</typeparam>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(T)</c> if found and deserialized successfully;
    /// <c>Left(EncinaError)</c> with <see cref="SecretsErrors.DeserializationFailedCode"/> if deserialization fails.
    /// </returns>
    ValueTask<Either<EncinaError, T>> GetSecretAsync<T>(
        string secretName,
        CancellationToken cancellationToken = default) where T : class;
}
