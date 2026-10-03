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
/// A <c>[CryptoShredded]</c> property that fails any of these checks, or whose setter cannot be
/// compiled (for example a getter-only property assigned in a constructor), is not a field: it is
/// recorded as unencryptable (<see cref="GetUnencryptableProperties"/>) so that the serializer refuses
/// to serialize the type instead of storing the value in plaintext (#1646). The startup scan
/// (<c>CryptoShreddingAutoRegistrationHostedService</c>) rejects the same properties by name.
/// </para>
/// </remarks>
internal static class CryptoShreddedPropertyCache
{
    private static readonly ConcurrentDictionary<Type, TypeMetadata> Cache = new();

    /// <summary>
    /// Gets the crypto-shredded field descriptors for the specified event type.
    /// </summary>
    /// <param name="eventType">The event type to discover crypto-shredded properties on.</param>
    /// <returns>
    /// An array of <see cref="CryptoShreddedFieldInfo"/> for the correctly configured properties decorated
    /// with <see cref="CryptoShreddedAttribute"/>. Returns an empty array if the type has none.
    /// </returns>
    internal static CryptoShreddedFieldInfo[] GetFields(Type eventType) => GetMetadata(eventType).Fields;

    /// <summary>
    /// Gets the names of the <c>[CryptoShredded]</c> properties of the specified type that cannot be
    /// encrypted because they are misconfigured (see the class remarks).
    /// </summary>
    /// <param name="eventType">The event type to inspect.</param>
    /// <returns>The property names, or an empty array when every <c>[CryptoShredded]</c> property is usable.</returns>
    internal static string[] GetUnencryptableProperties(Type eventType) => GetMetadata(eventType).UnencryptableProperties;

    /// <summary>
    /// Checks whether the specified event type has any correctly configured property decorated with
    /// <see cref="CryptoShreddedAttribute"/>.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>
    /// <c>true</c> if the type has at least one encryptable crypto-shredded property; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This is the fast-path check of the decryption paths, which only ever touch encryptable fields.
    /// The serialization paths use <see cref="HasCryptoShreddedProperties"/> instead.
    /// </remarks>
    internal static bool HasCryptoShreddedFields(Type eventType) => GetFields(eventType).Length > 0;

    /// <summary>
    /// Checks whether the specified event type declares any property decorated with
    /// <see cref="CryptoShreddedAttribute"/>, encryptable or not.
    /// </summary>
    /// <param name="eventType">The event type to check.</param>
    /// <returns>
    /// <c>true</c> if serializing the type must go through encryption (or fail because a property is
    /// unencryptable); <c>false</c> if the type carries no crypto-shredded data.
    /// </returns>
    internal static bool HasCryptoShreddedProperties(Type eventType) => GetMetadata(eventType).HasAnyProperty;

    /// <summary>
    /// Determines whether a compiled setter can be built for the property, which is what the serializer
    /// needs to overwrite the plaintext with its ciphertext. Used by the startup scan.
    /// </summary>
    /// <param name="ownerType">The type that declares the property.</param>
    /// <param name="property">The property to check.</param>
    /// <returns><c>true</c> if the property can be written; otherwise, <c>false</c>.</returns>
    internal static bool CanSetProperty(Type ownerType, PropertyInfo property) =>
        CompileSetter(ownerType, property) is not null;

    private static TypeMetadata GetMetadata(Type eventType) =>
        Cache.GetOrAdd(eventType, static t => DiscoverProperties(t));

    /// <summary>
    /// Discovers all properties on the given type that are decorated with <see cref="CryptoShreddedAttribute"/>
    /// and builds compiled setter delegates for each; misconfigured ones are recorded as unencryptable.
    /// </summary>
    private static TypeMetadata DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var cryptoShredded = new List<CryptoShreddedFieldInfo>();
        var unencryptable = new List<string>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<CryptoShreddedAttribute>();
            if (attribute is null)
            {
                continue;
            }

            var field = TryCreateField(type, property, attribute);
            if (field is null)
            {
                unencryptable.Add(property.Name);
            }
            else
            {
                cryptoShredded.Add(field);
            }
        }

        return new TypeMetadata([.. cryptoShredded], [.. unencryptable]);
    }

    private static CryptoShreddedFieldInfo? TryCreateField(Type type, PropertyInfo property, CryptoShreddedAttribute attribute)
    {
        if (!IsEncryptableProperty(property))
        {
            return null;
        }

        var subjectIdProperty = FindReadableSubjectIdProperty(type, attribute);
        if (subjectIdProperty is null)
        {
            return null;
        }

        // A property without a usable setter (e.g. getter-only) cannot receive its ciphertext.
        var setter = CompileSetter(type, property);
        return setter is null ? null : new CryptoShreddedFieldInfo(property, attribute, setter, subjectIdProperty);
    }

    // SubjectIdProperty must reference a valid, readable property (its type is checked at startup
    // and converted by SubjectIdConversion at serialization time).
    private static PropertyInfo? FindReadableSubjectIdProperty(Type type, CryptoShreddedAttribute attribute)
    {
        var subjectIdProperty = type.GetProperty(
            attribute.SubjectIdProperty,
            BindingFlags.Public | BindingFlags.Instance);

        return subjectIdProperty is { CanRead: true } ? subjectIdProperty : null;
    }

    // The property must be readable, a string (only strings can be encrypted) and carry [PersonalData].
    private static bool IsEncryptableProperty(PropertyInfo property) =>
        property.CanRead
        && property.PropertyType == typeof(string)
        && property.GetCustomAttribute<PersonalDataAttribute>() is not null;

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
            // The property has no setter usable from an expression tree (e.g. a getter-only property);
            // the caller records it as unencryptable so serialization fails closed (#1646).
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

    /// <summary>
    /// The discovery result for one type: its encryptable fields and the names of its unencryptable
    /// <c>[CryptoShredded]</c> properties.
    /// </summary>
    private sealed record TypeMetadata(CryptoShreddedFieldInfo[] Fields, string[] UnencryptableProperties)
    {
        public bool HasAnyProperty { get; } = Fields.Length > 0 || UnencryptableProperties.Length > 0;
    }
}
