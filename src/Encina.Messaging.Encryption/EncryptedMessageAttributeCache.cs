using System.Collections.Concurrent;
using System.Reflection;
using Encina.Messaging.Encryption.Attributes;

namespace Encina.Messaging.Encryption;

/// <summary>
/// Thread-safe static cache for discovering and caching <see cref="EncryptedMessageAttribute"/>
/// metadata on message types.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> to ensure each type is analyzed
/// exactly once via reflection. Subsequent lookups return the cached result with zero overhead.
/// </para>
/// <para>
/// Returns <c>null</c> for types that do not have the <see cref="EncryptedMessageAttribute"/>,
/// allowing callers to distinguish between "not decorated" and "decorated with Enabled = false".
/// </para>
/// </remarks>
internal static class EncryptedMessageAttributeCache
{
    private static readonly ConcurrentDictionary<Type, EncryptedMessageInfo?> Cache = new();

    /// <summary>
    /// Gets the encryption metadata for the specified message type.
    /// </summary>
    /// <param name="messageType">The message type to inspect for <see cref="EncryptedMessageAttribute"/>.</param>
    /// <returns>
    /// An <see cref="EncryptedMessageInfo"/> if the type is decorated with
    /// <see cref="EncryptedMessageAttribute"/>; otherwise, <c>null</c>.
    /// </returns>
    internal static EncryptedMessageInfo? GetEncryptionInfo(Type messageType)
    {
        return Cache.GetOrAdd(messageType, static t => DiscoverAttribute(t));
    }

    private static EncryptedMessageInfo? DiscoverAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<EncryptedMessageAttribute>(inherit: true);
        if (attribute is null)
        {
            return null;
        }

        return new EncryptedMessageInfo(attribute.Enabled, attribute.KeyId, attribute.UseTenantKey);
    }

    /// <summary>
    /// Clears the cached attribute descriptors. Intended for test isolation only.
    /// </summary>
    internal static void ClearCache()
    {
        Cache.Clear();
    }
}

/// <summary>
/// Cached metadata from an <see cref="EncryptedMessageAttribute"/> discovered on a message type.
/// </summary>
/// <param name="Enabled">Whether encryption is enabled for this message type.</param>
/// <param name="KeyId">The specific key identifier for this type, or <c>null</c> for the default key.</param>
/// <param name="UseTenantKey">Whether to use tenant-specific encryption keys.</param>
internal sealed record EncryptedMessageInfo(bool Enabled, string? KeyId, bool UseTenantKey);
