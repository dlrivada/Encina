using LanguageExt;

namespace Encina.Security.Secrets.Abstractions;

/// <summary>
/// Handles secret rotation callbacks. Implement this interface to react when
/// a secret is rotated by the vault or rotation coordinator.
/// </summary>
/// <remarks>
/// <para>
/// This follows a reactive model: the vault notifies the application when a secret is rotated,
/// and the handler executes the necessary actions (e.g., refresh connection pools, update caches).
/// </para>
/// <para>
/// Register handlers via <c>services.AddSecretRotationHandler&lt;THandler&gt;("secret-name")</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DatabaseCredentialRotationHandler : ISecretRotationHandler
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, string&gt;&gt; GenerateNewSecretAsync(
///         string secretName, CancellationToken ct)
///     {
///         return GenerateSecurePassword();
///     }
///
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; OnRotationAsync(
///         string secretName, string oldValue, string newValue, CancellationToken ct)
///     {
///         await RefreshConnectionPoolAsync(newValue, ct);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface ISecretRotationHandler
{
    /// <summary>
    /// Generates a new secret value for rotation.
    /// </summary>
    /// <param name="secretName">The name of the secret being rotated.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(newValue)</c> with the generated secret value;
    /// <c>Left(EncinaError)</c> if generation fails.
    /// </returns>
    ValueTask<Either<EncinaError, string>> GenerateNewSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Called after a secret has been successfully rotated.
    /// </summary>
    /// <param name="secretName">The name of the rotated secret.</param>
    /// <param name="oldValue">The previous secret value.</param>
    /// <param name="newValue">The new secret value.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> if the handler executed successfully;
    /// <c>Left(EncinaError)</c> if the handler failed.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> OnRotationAsync(
        string secretName,
        string oldValue,
        string newValue,
        CancellationToken cancellationToken = default);
}
