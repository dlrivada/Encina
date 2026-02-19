using LanguageExt;

namespace Encina.Secrets;

/// <summary>
/// Defines the contract for secret management operations using Railway Oriented Programming.
/// </summary>
/// <remarks>
/// <para>
/// Implementations provide access to secrets stored in external vaults or secret managers
/// such as Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, or Google Secret Manager.
/// </para>
/// <para>
/// <b>Error handling:</b> All methods return <c>Either&lt;EncinaError, T&gt;</c> following
/// Encina's Railway Oriented Programming pattern. Errors are represented as <c>Left</c>
/// values using codes defined in <see cref="SecretsErrorCodes"/>. Successful results are
/// <c>Right</c> values.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Retrieve a secret using ROP
/// var result = await provider.GetSecretAsync("my-api-key", ct);
/// result.Match(
///     Right: secret => Console.WriteLine($"Secret: {secret.Value}"),
///     Left: error => Console.WriteLine($"Error: {error.Message}"));
///
/// // Store a secret with expiration
/// var metadata = await provider.SetSecretAsync("db-password", "p@ssw0rd",
///     new SecretOptions(ExpiresAtUtc: DateTime.UtcNow.AddDays(90)), ct);
/// </code>
/// </example>
public interface ISecretProvider
{
    /// <summary>
    /// Gets a secret by name, returning the latest version.
    /// </summary>
    /// <param name="name">The name of the secret to retrieve.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Secret)</c> if found;
    /// <c>Left(EncinaError)</c> with code <see cref="SecretsErrorCodes.NotFoundCode"/> if not found,
    /// or other error codes for provider failures.
    /// </returns>
    ValueTask<Either<EncinaError, Secret>> GetSecretAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a secret.
    /// </summary>
    /// <param name="name">The name of the secret to retrieve.</param>
    /// <param name="version">The version identifier of the secret.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Secret)</c> if found;
    /// <c>Left(EncinaError)</c> with code <see cref="SecretsErrorCodes.VersionNotFoundCode"/> if the version does not exist.
    /// </returns>
    ValueTask<Either<EncinaError, Secret>> GetSecretVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a secret with the specified value.
    /// </summary>
    /// <param name="name">The name of the secret to set.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="options">Optional settings such as expiration and tags.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(SecretMetadata)</c> with the created/updated secret metadata;
    /// <c>Left(EncinaError)</c> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, SecretMetadata>> SetSecretAsync(string name, string value, SecretOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret by name.
    /// </summary>
    /// <param name="name">The name of the secret to delete.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success;
    /// <c>Left(EncinaError)</c> with code <see cref="SecretsErrorCodes.NotFoundCode"/> if the secret does not exist,
    /// or other error codes for provider failures.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> DeleteSecretAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists the names of all secrets available in the provider.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(IEnumerable&lt;string&gt;)</c> with secret names;
    /// <c>Left(EncinaError)</c> on provider failure.
    /// </returns>
    ValueTask<Either<EncinaError, IEnumerable<string>>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a secret with the specified name exists.
    /// </summary>
    /// <param name="name">The name of the secret to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(true)</c> if the secret exists; <c>Right(false)</c> if it does not;
    /// <c>Left(EncinaError)</c> on provider failure.
    /// </returns>
    ValueTask<Either<EncinaError, bool>> ExistsAsync(string name, CancellationToken cancellationToken = default);
}
