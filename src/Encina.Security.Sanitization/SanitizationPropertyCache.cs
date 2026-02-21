using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Encina.Security.Sanitization.Attributes;

namespace Encina.Security.Sanitization;

/// <summary>
/// Describes a property that requires sanitization, including its attribute metadata
/// and compiled getter/setter delegates for high-performance access.
/// </summary>
/// <param name="Property">The property info.</param>
/// <param name="Attribute">The sanitization attribute applied to the property.</param>
/// <param name="Getter">Compiled getter delegate: <c>(object instance) => object? value</c>.</param>
/// <param name="Setter">Compiled setter delegate: <c>(object instance, object? value) => void</c>.</param>
internal sealed record SanitizablePropertyInfo(
    PropertyInfo Property,
    SanitizationAttribute Attribute,
    Func<object, object?> Getter,
    Action<object, object?> Setter);

/// <summary>
/// Thread-safe static cache for discovering and caching properties decorated with
/// <see cref="SanitizationAttribute"/> subclasses on a per-type basis.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> with
/// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
/// to ensure each type is analyzed exactly once. Subsequent lookups return the cached result
/// without any reflection overhead.
/// </para>
/// <para>
/// Property getters and setters are compiled from expression trees at discovery time,
/// providing near-native performance for reading and writing property values.
/// </para>
/// </remarks>
internal static class SanitizationPropertyCache
{
    private static readonly ConcurrentDictionary<Type, SanitizablePropertyInfo[]> Cache = new();

    /// <summary>
    /// Gets the sanitizable property descriptors for the specified type.
    /// </summary>
    /// <param name="type">The type to discover sanitizable properties on.</param>
    /// <returns>
    /// An array of <see cref="SanitizablePropertyInfo"/> for properties decorated with
    /// any <see cref="SanitizationAttribute"/> subclass. Returns an empty array if the type
    /// has no sanitizable properties.
    /// </returns>
    internal static SanitizablePropertyInfo[] GetProperties(Type type)
    {
        return Cache.GetOrAdd(type, static t => DiscoverProperties(t));
    }

    /// <summary>
    /// Gets the string property descriptors for auto-sanitization mode
    /// (all string properties, no attribute required).
    /// </summary>
    /// <param name="type">The type to discover string properties on.</param>
    /// <returns>
    /// An array of <see cref="PropertyInfo"/> for all public instance string properties
    /// that have both a getter and setter.
    /// </returns>
    internal static PropertyInfo[] GetStringProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite)
            .ToArray();
    }

    /// <summary>
    /// Discovers all properties on the given type that are decorated with any
    /// <see cref="SanitizationAttribute"/> subclass and builds compiled delegates.
    /// </summary>
    private static SanitizablePropertyInfo[] DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var sanitizable = new List<SanitizablePropertyInfo>();

        foreach (var property in properties)
        {
            if (property.PropertyType != typeof(string))
            {
                continue;
            }

            var attribute = property.GetCustomAttribute<SanitizationAttribute>(inherit: true);
            if (attribute is null)
            {
                continue;
            }

            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            var getter = CompileGetter(type, property);
            var setter = CompileSetter(type, property);

            if (getter is null || setter is null)
            {
                continue;
            }

            sanitizable.Add(new SanitizablePropertyInfo(property, attribute, getter, setter));
        }

        return [.. sanitizable];
    }

    /// <summary>
    /// Compiles a fast getter delegate from an expression tree.
    /// </summary>
    private static Func<object, object?>? CompileGetter(Type ownerType, PropertyInfo property)
    {
        try
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var castTarget = Expression.Convert(targetParam, ownerType);
            var propertyAccess = Expression.Property(castTarget, property);
            var castResult = Expression.Convert(propertyAccess, typeof(object));

            var lambda = Expression.Lambda<Func<object, object?>>(castResult, targetParam);
            return lambda.Compile();
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Compiles a fast setter delegate from an expression tree.
    /// </summary>
    private static Action<object, object?>? CompileSetter(Type ownerType, PropertyInfo property)
    {
        try
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var castTarget = Expression.Convert(targetParam, ownerType);
            var castValue = Expression.Convert(valueParam, property.PropertyType);
            var propertyAccess = Expression.Property(castTarget, property);
            var assignment = Expression.Assign(propertyAccess, castValue);

            var lambda = Expression.Lambda<Action<object, object?>>(assignment, targetParam, valueParam);
            return lambda.Compile();
        }
        catch (ArgumentException)
        {
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
