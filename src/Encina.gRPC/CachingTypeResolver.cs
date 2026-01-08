using System.Collections.Concurrent;

namespace Encina.gRPC;

/// <summary>
/// A type resolver that caches resolved types for improved performance.
/// </summary>
public sealed class CachingTypeResolver : ITypeResolver
{
    private readonly ConcurrentDictionary<string, Type?> _requestTypeCache = new();
    private readonly ConcurrentDictionary<string, Type?> _notificationTypeCache = new();

    /// <inheritdoc />
    public Type? ResolveRequestType(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        return ResolveType(typeName, _requestTypeCache);
    }

    /// <inheritdoc />
    public Type? ResolveNotificationType(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        return ResolveType(typeName, _notificationTypeCache);
    }

    private static Type? ResolveType(string typeName, ConcurrentDictionary<string, Type?> cache)
    {
        return cache.GetOrAdd(typeName, static name => Type.GetType(name));
    }
}
