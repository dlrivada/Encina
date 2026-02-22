using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Encina.Security.Secrets.Injection;

/// <summary>
/// Thread-safe static cache for discovering and caching properties decorated with
/// <see cref="InjectSecretAttribute"/> on a per-type basis.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> with <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
/// to ensure each type is analyzed exactly once. Subsequent lookups return the cached result
/// without any reflection overhead.
/// </para>
/// <para>
/// Property setters are compiled from expression trees at discovery time, providing
/// near-native performance for setting secret values on target objects.
/// </para>
/// </remarks>
internal static class SecretPropertyCache
{
    private static readonly ConcurrentDictionary<Type, SecretPropertyInfo[]> Cache = new();

    /// <summary>
    /// Gets the injectable property descriptors for the specified type.
    /// </summary>
    /// <param name="type">The type to discover injectable properties on.</param>
    /// <returns>
    /// An array of <see cref="SecretPropertyInfo"/> for properties decorated with
    /// <see cref="InjectSecretAttribute"/>. Returns an empty array if the type has no injectable properties.
    /// </returns>
    internal static SecretPropertyInfo[] GetProperties(Type type)
    {
        return Cache.GetOrAdd(type, static t => DiscoverProperties(t));
    }

    /// <summary>
    /// Discovers all properties on the given type that are decorated with <see cref="InjectSecretAttribute"/>
    /// and builds compiled setter delegates for each.
    /// </summary>
    private static SecretPropertyInfo[] DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var injectable = new List<SecretPropertyInfo>();

        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<InjectSecretAttribute>();
            if (attribute is null)
            {
                continue;
            }

            // Only support writable string properties
            if (!property.CanWrite || property.PropertyType != typeof(string))
            {
                continue;
            }

            var setter = CompileSetter(type, property);
            if (setter is null)
            {
                continue;
            }

            injectable.Add(new SecretPropertyInfo(property, attribute, setter));
        }

        return [.. injectable];
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
