using LanguageExt;

namespace Encina.Security.Secrets.Abstractions;

/// <summary>
/// Defines the contract for writing secrets using Railway Oriented Programming.
/// </summary>
/// <remarks>
/// <para>
/// Separate from <see cref="ISecretReader"/> following Interface Segregation Principle.
/// Development providers (environment variables, configuration) typically do not implement this interface.
/// </para>
/// <para>
/// <b>Thread safety:</b> Implementations must be thread-safe for concurrent access.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Custom vault provider implementing both read and write
/// public class VaultProvider : ISecretReader, ISecretWriter
/// {
///     public async ValueTask&lt;Either&lt;EncinaError, Unit&gt;&gt; SetSecretAsync(
///         string secretName, string value, CancellationToken ct)
///     {
///         await vault.WriteAsync(secretName, value);
///         return Unit.Default;
///     }
/// }
/// </code>
/// </example>
public interface ISecretWriter
{
    /// <summary>
    /// Creates or updates a secret with the specified value.
    /// </summary>
    /// <param name="secretName">The name of the secret to set.</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// <c>Right(Unit)</c> on success;
    /// <c>Left(EncinaError)</c> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> SetSecretAsync(
        string secretName,
        string value,
        CancellationToken cancellationToken = default);
}
