using LanguageExt;

namespace Encina.Security.Secrets.Abstractions;

/// <summary>
/// Defines the contract for rotating secrets using Railway Oriented Programming.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="ISecretReader"/> and <see cref="ISecretWriter"/> following
/// Interface Segregation Principle. Only vault providers that support rotation implement this.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe for concurrent access.
/// </para>
/// </remarks>
public interface ISecretRotator
{
    /// <summary>
    /// Rotates a secret, generating a new version.
    /// </summary>
    /// <param name="secretName">The name of the secret to rotate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on successful rotation;
    /// <c>Left(EncinaError)</c> with code <see cref="SecretsErrors.RotationFailedCode"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> RotateSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}
