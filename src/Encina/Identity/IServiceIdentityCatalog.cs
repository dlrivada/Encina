using System.Diagnostics.CodeAnalysis;

namespace Encina;

/// <summary>
/// Reads the service identities declared at startup.
/// </summary>
/// <remarks>
/// Declarations are made with <c>AddEncinaServiceIdentity</c> and are fixed once the host starts.
/// The catalog never creates an identity: scopes are opened through
/// <see cref="IRequestContextScopeFactory"/>.
/// </remarks>
/// <example>
/// <code>
/// if (!catalog.TryGet("billing-reconciliation", out var definition))
/// {
///     throw new InvalidOperationException("Declare the billing job identity at startup.");
/// }
/// </code>
/// </example>
public interface IServiceIdentityCatalog
{
    /// <summary>
    /// Finds the declaration of <paramref name="name"/> (ordinal, case-sensitive).
    /// </summary>
    /// <param name="name">The declared name.</param>
    /// <param name="definition">The declaration when found; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="name"/> is declared.</returns>
    bool TryGet(string name, [MaybeNullWhen(false)] out ServiceIdentityDefinition definition);
}
