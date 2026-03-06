using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

using Encina.Compliance.DataSubjectRights;

namespace Encina.Marten.GDPR;

/// <summary>
/// Thread-safe static cache for discovering and caching properties decorated with
/// <see cref="CryptoShreddedAttribute"/> on a per-type basis.
/// </summary>
/// <remarks>
/// <para>
/// Follows the same pattern as <c>EncryptedPropertyCache</c> from
/// <c>Encina.Security.Encryption</c>: uses <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// with <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
/// to ensure each type is analyzed exactly once. Subsequent lookups return the cached result
/// without any reflection overhead.
/// </para>
/// <para>
/// Property setters are compiled from expression trees at discovery time, providing
/// near-native performance for setting encrypted values on target objects.
/// </para>
/// <para>
/// Discovery validates that:
/// </para>
/// <list type="bullet">
/// <item><description>The <c>[CryptoShredded]</c> attribute co-exists with
/// <c>[PersonalData]</c> from <c>Encina.Compliance.DataSubjectRights</c></description></item>
/// <item><description>The <see cref="CryptoShreddedAttribute.SubjectIdProperty"/> refers
/// to a valid, readable property on the declaring type</description></item>
/// <item><description>The target property is a <c>string</c> type (only strings can be
/// encrypted for crypto-shredding)</description></item>
/// </list>
/// <para>
/// Misconfigured properties are excluded from the cache silently. Configuration errors
/// can be detected at application startup by inspecting <see cref="GetFields"/> results.
/// </para>
/// </remarks>
internal static class CryptoShreddedPropertyCache
{
    private static readonly ConcurrentDictionary<Type, CryptoShreddedFieldInfo[]> Cache = new();

    /// <summary>
    /// Gets the crypto-shredded field descriptors for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type to discover crypto-shredded properties on.</param>
    /// <returns>
    /// An array of <see cref="CryptoShreddedFieldInfo"/> for properties decorated with
    /// <see cref="CryptoShreddedAttribute"/>. Returns an empty array if the type has no
    /// crypto-shredded properties.
    /// </returns>
    internal static CryptoShreddedFieldInfo[] GetFields(Type eventType)
    {
        return Cache.GetOrAdd(eventType, static t => DiscoverProperties(t));
    }

    /// <summary>
    /// Checks whether the specified event type has any properties decorated with
    /// <see cref="CryptoShreddedAttribute"/>.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>
    /// <c>true</c> if the type has at least one crypto-shredded property; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is a fast-path check used by the serializer to skip encryption/decryption
    /// processing for events that have no PII fields.
    /// </remarks>
    internal static bool HasCryptoShreddedFields(Type eventType)
    {
        return GetFields(eventType).Length > 0;
    }

    /// <summary>
    /// Discovers all properties on the given type that are decorated with <see cref="CryptoShreddedAttribute"/>
    /// and builds compiled setter delegates for each.
    /// </summary>
    private static CryptoShreddedFieldInfo[] DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var cryptoShredded = new List<CryptoShreddedFieldInfo>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<CryptoShreddedAttribute>();
            if (attribute is null)
            {
                continue;
            }

            // Validate: property must be readable
            if (!property.CanRead)
            {
                continue;
            }

            // Validate: property must be a string (only strings can be encrypted for crypto-shredding)
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            // Validate: [PersonalData] must co-exist
            var personalDataAttr = property.GetCustomAttribute<PersonalDataAttribute>();
            if (personalDataAttr is null)
            {
                continue;
            }

            // Validate: SubjectIdProperty must reference a valid, readable string property
            var subjectIdProperty = type.GetProperty(
                attribute.SubjectIdProperty,
                BindingFlags.Public | BindingFlags.Instance);

            if (subjectIdProperty is null || !subjectIdProperty.CanRead)
            {
                continue;
            }

            // Compile a fast setter delegate
            var setter = CompileSetter(type, property);
            if (setter is null)
            {
                continue;
            }

            cryptoShredded.Add(new CryptoShreddedFieldInfo(
                property,
                attribute,
                setter,
                attribute.SubjectIdProperty));
        }

        return [.. cryptoShredded];
    }

    /// <summary>
    /// Compiles a fast setter delegate from an expression tree for the specified property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generates the equivalent of:
    /// <code>
    /// (object target, object? value) => ((TOwner)target).Property = (TProperty)value;
    /// </code>
    /// </para>
    /// <para>
    /// This avoids the overhead of <see cref="PropertyInfo.SetValue(object?, object?)"/>
    /// which uses reflection on every call.
    /// </para>
    /// </remarks>
    private static Action<object, object?>? CompileSetter(Type ownerType, PropertyInfo property)
    {
        try
        {
            // Parameters: (object target, object? value)
            var targetParam = Expression.Parameter(typeof(object), "target");
            var valueParam = Expression.Parameter(typeof(object), "value");

            // (TOwner)target
            var castTarget = Expression.Convert(targetParam, ownerType);

            // (TProperty)value — handles nullable types via Convert
            var castValue = Expression.Convert(valueParam, property.PropertyType);

            // ((TOwner)target).Property = (TProperty)value
            var propertyAccess = Expression.Property(castTarget, property);
            var assignment = Expression.Assign(propertyAccess, castValue);

            // Compile to delegate
            var lambda = Expression.Lambda<Action<object, object?>>(
                assignment,
                targetParam,
                valueParam);

            return lambda.Compile();
        }
        catch (ArgumentException)
        {
            // Property may not have a setter accessible via expression trees
            // (e.g., init-only properties in some scenarios)
            return null;
        }
    }

    /// <summary>
    /// Gets whether any event types have been registered in the cache.
    /// </summary>
    /// <remarks>
    /// Used by the health check to detect if auto-registration has run or
    /// any events have been serialized.
    /// </remarks>
    internal static bool HasAnyRegisteredTypes => !Cache.IsEmpty;

    /// <summary>
    /// Gets the number of event types currently cached.
    /// </summary>
    /// <remarks>
    /// Used by health checks and diagnostics.
    /// </remarks>
    internal static int CachedTypeCount => Cache.Count;

    /// <summary>
    /// Clears the cached property descriptors. Intended for test isolation only.
    /// </summary>
    internal static void ClearCache()
    {
        Cache.Clear();
    }
}
