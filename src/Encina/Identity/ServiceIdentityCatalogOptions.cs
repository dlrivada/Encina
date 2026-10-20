namespace Encina;

/// <summary>
/// The service identities declared at startup (<c>AddEncinaServiceIdentity</c> and the built-in
/// declarations of Encina packages), validated by <see cref="ServiceIdentityCatalogOptionsValidator"/>.
/// </summary>
/// <remarks>
/// Internal: declarations are made only through the registration methods. A repeated identical
/// declaration is ignored; a different declaration under a name already used is recorded as a
/// conflict and fails startup.
/// </remarks>
internal sealed class ServiceIdentityCatalogOptions
{
    private readonly Dictionary<string, ServiceIdentityDefinition> _identities = new(StringComparer.Ordinal);
    private readonly System.Collections.Generic.HashSet<string> _conflicts = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the declared identities by name.
    /// </summary>
    internal IReadOnlyDictionary<string, ServiceIdentityDefinition> Identities => _identities;

    /// <summary>
    /// Gets the names declared more than once with different content.
    /// </summary>
    internal IReadOnlyCollection<string> ConflictingNames => _conflicts;

    /// <summary>
    /// Adds <paramref name="definition"/>, ignoring an identical repeat and recording a conflicting one.
    /// </summary>
    internal void Declare(ServiceIdentityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (!_identities.TryGetValue(definition.Name, out var existing))
        {
            _identities.Add(definition.Name, definition);
            return;
        }

        if (!existing.IsEquivalentTo(definition))
        {
            _conflicts.Add(definition.Name);
        }
    }
}
