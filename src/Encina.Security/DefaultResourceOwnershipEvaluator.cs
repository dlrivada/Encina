using System.Collections.Concurrent;
using System.Reflection;

namespace Encina.Security;

/// <summary>
/// Default implementation of <see cref="IResourceOwnershipEvaluator"/> that uses
/// cached reflection to compare a property value with the current user's identity.
/// </summary>
/// <remarks>
/// <para>
/// Property lookups are cached in a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// keyed by <c>(Type, string)</c> to avoid repeated reflection overhead,
/// consistent with the caching pattern used in the audit module.
/// </para>
/// <para>
/// Returns <c>false</c> when:
/// <list type="bullet">
/// <item><description>The property does not exist on the resource type</description></item>
/// <item><description>The property value is <c>null</c></description></item>
/// <item><description>The <see cref="ISecurityContext.UserId"/> is <c>null</c></description></item>
/// <item><description>The property value does not match the user ID</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class DefaultResourceOwnershipEvaluator : IResourceOwnershipEvaluator
{
    private static readonly ConcurrentDictionary<(Type Type, string Property), PropertyInfo?> PropertyCache = new();

    /// <inheritdoc />
    public ValueTask<bool> IsOwnerAsync<TResource>(
        ISecurityContext context,
        TResource resource,
        string propertyName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (context.UserId is null)
        {
            return ValueTask.FromResult(false);
        }

        var resourceType = typeof(TResource);
        var property = PropertyCache.GetOrAdd(
            (resourceType, propertyName),
            static key => key.Type.GetProperty(key.Property, BindingFlags.Public | BindingFlags.Instance));

        if (property is null)
        {
            return ValueTask.FromResult(false);
        }

        var propertyValue = property.GetValue(resource)?.ToString();

        var isOwner = string.Equals(propertyValue, context.UserId, StringComparison.Ordinal);
        return ValueTask.FromResult(isOwner);
    }
}
