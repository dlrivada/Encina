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
/// Returns <c>null</c> for types that do not have the <see cref="EncryptedMessageAttribute"/>
/// (declared or inherited from a base type). A non-<c>null</c> result means the type must be
/// encrypted: the attribute carries no value that disables encryption.
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

        return new EncryptedMessageInfo(attribute.KeyId, attribute.UseTenantKey);
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
/// Its presence alone means the type is always encrypted.
/// </summary>
/// <param name="KeyId">The specific key identifier for this type, or <c>null</c> for the default key.</param>
/// <param name="UseTenantKey">The attribute's <c>UseTenantKey</c> value; not consumed yet.</param>
internal sealed record EncryptedMessageInfo(string? KeyId, bool UseTenantKey);
