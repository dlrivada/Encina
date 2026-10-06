using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// The default <see cref="IServiceIdentityCatalog"/>: the validated declarations, frozen once.
/// </summary>
internal sealed class ServiceIdentityCatalog : IServiceIdentityCatalog
{
    private readonly FrozenDictionary<string, ServiceIdentityDefinition> _identities;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceIdentityCatalog"/> class.
    /// </summary>
    /// <param name="options">The declarations.</param>
    public ServiceIdentityCatalog(IOptions<ServiceIdentityCatalogOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _identities = options.Value.Identities.ToFrozenDictionary(StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public bool TryGet(string name, [MaybeNullWhen(false)] out ServiceIdentityDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(name);
        return _identities.TryGetValue(name, out definition);
    }
}
