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
        var classLogOnlyAttribute = type.GetCustomAttribute<MaskInLogsAttribute>(inherit: true);

        foreach (var property in properties)
        {
            if (!property.CanRead)
            {
                continue;
            }

            if (property.PropertyType != typeof(string))
            {
                // A masking attribute on a value that cannot be masked must not be dropped silently:
                // masking fails closed on it (in every context for [PII]/[SensitiveData], in the log
                // context for [MaskInLogs]).
                if (FromUnmaskable(property) is { } unmaskable)
                {
                    piiProperties.Add(unmaskable);
                }

                continue;
            }

            // A property without a usable setter (get-only) is still PII: it is kept and the masker
            // fails closed when the masked value does not take effect on the copy.
            var setter = CompileSetter(type, property);

            var metadata = ResolveMetadata(
                property, setter, classAttribute, classSensitiveAttribute, classLogOnlyAttribute);
            if (metadata is not null)
            {
                piiProperties.Add(metadata.Value);
            }
        }

        return [.. piiProperties];
    }

    // Precedence: an always-masked attribute ([PII], [SensitiveData]) wins over the log-only
    // [MaskInLogs] at any level, so adding [MaskInLogs] can never stop masking in responses or audit;
    // within each kind the property-level attribute wins over the class-level one.
    private static PropertyMaskingMetadata? ResolveMetadata(
        PropertyInfo property,
        Action<object, object?>? setter,
        PIIAttribute? classAttribute,
        SensitiveDataAttribute? classSensitiveAttribute,
        MaskInLogsAttribute? classLogOnlyAttribute) =>
        FromPropertyAlwaysMasked(property, setter)
        ?? FromClassAttributes(property, setter, classAttribute, classSensitiveAttribute)
        ?? FromLogOnly(property, setter, property.GetCustomAttribute<MaskInLogsAttribute>(inherit: true))
        ?? FromLogOnly(property, setter, classLogOnlyAttribute);

    private static PropertyMaskingMetadata? FromPropertyAlwaysMasked(
        PropertyInfo property,
        Action<object, object?>? setter)
    {
        var piiAttr = property.GetCustomAttribute<PIIAttribute>(inherit: true);
        if (piiAttr is not null)
        {
            return new PropertyMaskingMetadata(
                property, piiAttr.Type, piiAttr.Mode, piiAttr.Pattern, piiAttr.Replacement, logOnly: false, setter);
        }

        var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);
        return sensitiveAttr is null
            ? null
            : new PropertyMaskingMetadata(
                property, PIIType.Custom, sensitiveAttr.Mode, pattern: null, replacement: null, logOnly: false, setter);
    }

    // A non-string property cannot be masked. The property-level always-masked attribute ([PII], then
    // [SensitiveData]) wins over [MaskInLogs], as it does for strings, so the property is kept as
    // unmaskable in every context; a property with only [MaskInLogs] is unmaskable in the log context.
    // Class-level attributes never apply to non-string members.
    private static PropertyMaskingMetadata? FromUnmaskable(PropertyInfo property)
    {
        var piiAttr = property.GetCustomAttribute<PIIAttribute>(inherit: true);
        if (piiAttr is not null)
        {
            return new PropertyMaskingMetadata(
                property, piiAttr.Type, piiAttr.Mode, piiAttr.Pattern, piiAttr.Replacement,
                logOnly: false, setter: null, unmaskable: true);
        }

        var sensitiveAttr = property.GetCustomAttribute<SensitiveDataAttribute>(inherit: true);
        if (sensitiveAttr is not null)
        {
            return new PropertyMaskingMetadata(
                property, PIIType.Custom, sensitiveAttr.Mode, pattern: null, replacement: null,
                logOnly: false, setter: null, unmaskable: true);
        }

        var logOnlyAttr = property.GetCustomAttribute<MaskInLogsAttribute>(inherit: true);
        return logOnlyAttr is null
            ? null
            : new PropertyMaskingMetadata(
                property, PIIType.Custom, logOnlyAttr.Mode, pattern: null, replacement: null,
                logOnly: true, setter: null, unmaskable: true);
    }

    private static PropertyMaskingMetadata? FromLogOnly(
        PropertyInfo property,
        Action<object, object?>? setter,
        MaskInLogsAttribute? logOnlyAttr) =>
        logOnlyAttr is null
            ? null
            : new PropertyMaskingMetadata(
                property, PIIType.Custom, logOnlyAttr.Mode, pattern: null, replacement: null, logOnly: true, setter);

    // A class-level always-masked attribute applies to every string property of the type.
    private static PropertyMaskingMetadata? FromClassAttributes(
        PropertyInfo property,
        Action<object, object?>? setter,
        PIIAttribute? classAttribute,
        SensitiveDataAttribute? classSensitiveAttribute)
    {
        if (classAttribute is not null)
        {
            return new PropertyMaskingMetadata(
                property,
                classAttribute.Type,
                classAttribute.Mode,
                classAttribute.Pattern,
                classAttribute.Replacement,
                logOnly: false,
                setter);
        }

        return classSensitiveAttribute is null
            ? null
            : new PropertyMaskingMetadata(
                property,
                PIIType.Custom,
                classSensitiveAttribute.Mode,
                pattern: null,
                replacement: null,
                logOnly: false,
                setter);
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
