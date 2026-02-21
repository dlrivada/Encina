using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Encina.Security.PII.Attributes;

namespace Encina.Security.PII.Internal;

/// <summary>
/// Thread-safe static cache for discovering and caching properties decorated with
/// PII masking attributes on a per-type basis.
/// </summary>
/// <remarks>
/// <para>
/// Uses <see cref="ConcurrentDictionary{TKey, TValue}"/> with
/// <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>
/// to ensure each type is analyzed exactly once. Subsequent lookups return the cached result
/// without any reflection overhead.
/// </para>
/// <para>
/// Scans for <see cref="PIIAttribute"/>, <see cref="SensitiveDataAttribute"/>,
/// and <see cref="MaskInLogsAttribute"/>.
/// Property setters are compiled from expression trees at discovery time.
/// </para>
/// </remarks>
internal static class PIIPropertyScanner
{
    private static readonly ConcurrentDictionary<Type, PropertyMaskingMetadata[]> Cache = new();

    /// <summary>
    /// Gets the PII property metadata for the specified type.
    /// </summary>
    /// <param name="type">The type to discover PII-decorated properties on.</param>
    /// <returns>
    /// An array of <see cref="PropertyMaskingMetadata"/> for properties decorated with
    /// PII masking attributes. Returns an empty array if the type has no PII properties.
    /// </returns>
    internal static PropertyMaskingMetadata[] GetProperties(Type type)
    {
        return Cache.GetOrAdd(type, static t => DiscoverProperties(t));
    }

    /// <summary>
    /// Discovers all properties on the given type that are decorated with PII masking attributes.
    /// </summary>
    private static PropertyMaskingMetadata[] DiscoverProperties(Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var piiProperties = new List<PropertyMaskingMetadata>();

        // Check for class-level PII attribute
        var classAttribute = type.GetCustomAttribute<PIIAttribute>(inherit: true);
        var classSensitiveAttribute = type.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);

        foreach (var property in properties)
        {
            if (!property.CanRead || property.PropertyType != typeof(string))
            {
                continue;
            }

            var setter = CompileSetter(type, property);
            if (setter is null)
            {
                continue;
            }

            // Check property-level attributes (take priority over class-level)
            var piiAttr = property.GetCustomAttribute<PIIAttribute>(inherit: true);
            var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);
            var logOnlyAttr = property.GetCustomAttribute<MaskInLogsAttribute>(inherit: true);

            if (piiAttr is not null)
            {
                piiProperties.Add(new PropertyMaskingMetadata(
                    property,
                    piiAttr.Type,
                    piiAttr.Mode,
                    piiAttr.Pattern,
                    piiAttr.Replacement,
                    logOnly: false,
                    setter));
            }
            else if (sensitiveAttr is not null)
            {
                piiProperties.Add(new PropertyMaskingMetadata(
                    property,
                    PIIType.Custom,
                    sensitiveAttr.Mode,
                    pattern: null,
                    replacement: null,
                    logOnly: false,
                    setter));
            }
            else if (logOnlyAttr is not null)
            {
                piiProperties.Add(new PropertyMaskingMetadata(
                    property,
                    PIIType.Custom,
                    logOnlyAttr.Mode,
                    pattern: null,
                    replacement: null,
                    logOnly: true,
                    setter));
            }
            else if (classAttribute is not null)
            {
                // Class-level PII attribute applies to all string properties
                piiProperties.Add(new PropertyMaskingMetadata(
                    property,
                    classAttribute.Type,
                    classAttribute.Mode,
                    classAttribute.Pattern,
                    classAttribute.Replacement,
                    logOnly: false,
                    setter));
            }
            else if (classSensitiveAttribute is not null)
            {
                piiProperties.Add(new PropertyMaskingMetadata(
                    property,
                    PIIType.Custom,
                    classSensitiveAttribute.Mode,
                    pattern: null,
                    replacement: null,
                    logOnly: false,
                    setter));
            }
        }

        return [.. piiProperties];
    }

    /// <summary>
    /// Compiles a fast setter delegate from an expression tree for the specified property.
    /// </summary>
    /// <remarks>
    /// Generates the equivalent of:
    /// <code>
    /// (object target, object? value) => ((TOwner)target).Property = (TProperty)value;
    /// </code>
    /// </remarks>
    private static Action<object, object?>? CompileSetter(Type ownerType, PropertyInfo property)
    {
        if (!property.CanWrite)
        {
            return null;
        }

        try
        {
            var targetParam = Expression.Parameter(typeof(object), "target");
            var valueParam = Expression.Parameter(typeof(object), "value");

            var castTarget = Expression.Convert(targetParam, ownerType);
            var castValue = Expression.Convert(valueParam, property.PropertyType);

            var propertyAccess = Expression.Property(castTarget, property);
            var assignment = Expression.Assign(propertyAccess, castValue);

            var lambda = Expression.Lambda<Action<object, object?>>(
                assignment,
                targetParam,
                valueParam);

            return lambda.Compile();
        }
        catch (ArgumentException)
        {
            // Init-only or otherwise inaccessible setter
            return null;
        }
    }

    /// <summary>
    /// Clears the cached property metadata. Intended for test isolation only.
    /// </summary>
    internal static void ClearCache()
    {
        Cache.Clear();
    }
}
