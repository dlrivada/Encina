using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Encina.Security.Encryption;

/// <summary>
/// Thread-safe static cache for discovering and caching properties decorated with
/// <see cref="EncryptAttribute"/> on a per-type basis.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> with <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
/// to ensure each type is analyzed exactly once. Subsequent lookups return the cached result
/// without any reflection overhead.
/// </para>
/// <para>
/// Property setters are compiled from expression trees at discovery time, providing
/// near-native performance for setting encrypted values on target objects.
/// </para>
/// </remarks>
internal static class EncryptedPropertyCache
{
    private static readonly ConcurrentDictionary<Type, EncryptedPropertyInfo[]> Cache = new();

    /// <summary>
    /// Gets the encrypted property descriptors for the specified type.
    /// </summary>
    /// <param name="type">The type to discover encrypted properties on.</param>
    /// <returns>
    /// An array of <see cref="EncryptedPropertyInfo"/> for properties decorated with
    /// <see cref="EncryptAttribute"/>. Returns an empty array if the type has no encrypted properties.
    /// </returns>
    internal static EncryptedPropertyInfo[] GetProperties(Type type)
    {
        return Cache.GetOrAdd(type, static t => DiscoverProperties(t));
    }

    /// <summary>
    /// Discovers all properties on the given type that are decorated with <see cref="EncryptAttribute"/>
    /// and builds compiled setter delegates for each.
    /// </summary>
    private static EncryptedPropertyInfo[] DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var encrypted = new List<EncryptedPropertyInfo>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<EncryptAttribute>();
            if (attribute is null)
            {
                continue;
            }

            if (!property.CanRead)
            {
                continue;
            }

            var setter = property.CanWrite
                ? CompileSetter(type, property)
                : null;

            if (setter is null)
            {
                continue;
            }

            encrypted.Add(new EncryptedPropertyInfo(property, attribute, setter));
        }

        return [.. encrypted];
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
    /// Clears the cached property descriptors. Intended for test isolation only.
    /// </summary>
    internal static void ClearCache()
    {
        Cache.Clear();
    }
}
