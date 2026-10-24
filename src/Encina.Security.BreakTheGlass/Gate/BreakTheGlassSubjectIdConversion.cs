using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Encina.Security.BreakTheGlass;

/// <summary>
/// Converts the value of a subject-id property to a stable, culture-invariant string, following the
/// conversion rules of the data subject rights extractor (<c>IDataSubjectIdExtractor</c>).
/// </summary>
/// <remarks>
/// <para>
/// Supported shapes: <see cref="string"/> (as is), <see cref="Guid"/> (<c>"D"</c> format), the integer
/// types (invariant culture), strongly typed ids outside the base class library that implement
/// <see cref="IFormattable"/>, and wrappers (record, record struct, struct or class) that expose a public
/// instance <c>Value</c> property of one of the primitive types above; the <c>Value</c> is unwrapped. The
/// compiler-generated record <c>ToString()</c> and <see cref="ValueType.ToString()"/> are never used.
/// </para>
/// <para>
/// <c>null</c>, <see cref="Guid.Empty"/> and an empty or whitespace string mean "subject missing" and
/// return <c>null</c>. Any other type is a configuration error and throws
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// This is a copy of the rules of <c>Encina.Compliance.DataSubjectRights</c>; a shared core converter is
/// tracked by #1193 (OD-3). Change both together.
/// </para>
/// </remarks>
internal static class BreakTheGlassSubjectIdConversion
{
    private static readonly HashSet<Type> IntegerTypes =
    [
        typeof(sbyte),
        typeof(byte),
        typeof(short),
        typeof(ushort),
        typeof(int),
        typeof(uint),
        typeof(long),
        typeof(ulong),
        typeof(Int128),
        typeof(UInt128)
    ];

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> ValuePropertyCache = new();

    /// <summary>Converts a value read from a subject-id property to its invariant string form.</summary>
    /// <param name="value">The value read from the property.</param>
    /// <param name="property">The property, used for the error message.</param>
    /// <returns>The invariant string, or <c>null</c> when the subject is missing.</returns>
    /// <exception cref="InvalidOperationException">The value's type is not a supported subject-id shape.</exception>
    public static string? ToInvariantString(object? value, PropertyInfo property)
    {
        if (value is null)
        {
            return null;
        }

        if (TryConvertPrimitive(value, out var primitive))
        {
            return primitive;
        }

        var type = value.GetType();
        if (IsStronglyTypedId(type))
        {
            return ConvertStronglyTypedId(value);
        }

        throw new InvalidOperationException(
            $"Subject-id property '{property.DeclaringType?.Name}.{property.Name}' has type '{type.Name}', " +
            "which is not a supported subject identifier. Supported types are string, Guid, integer types, " +
            "strongly typed ids implementing IFormattable, and wrappers exposing a public 'Value' property of " +
            "one of those primitive types.");
    }

    /// <summary>Determines whether a property of the declared type can hold a supported subject id.</summary>
    /// <param name="type">The declared type of the subject-id property.</param>
    /// <returns><c>true</c> when the type is a primitive subject type or a strongly typed id.</returns>
    public static bool IsSupportedType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return IsPrimitiveSubjectType(underlying) || IsStronglyTypedId(underlying);
    }

    private static string? ConvertStronglyTypedId(object value)
    {
        // The wrapped value decides whether the subject is missing, even when the wrapper is IFormattable.
        var valueProperty = ValuePropertyCache.GetOrAdd(value.GetType(), ResolveValueProperty);
        if (valueProperty is not null)
        {
            var inner = valueProperty.GetValue(value);
            return inner is not null && TryConvertPrimitive(inner, out var unwrapped) ? unwrapped : null;
        }

        return NormalizeFormatted(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
    }

    private static bool IsPrimitiveSubjectType(Type type) =>
        type == typeof(string) || type == typeof(Guid) || IntegerTypes.Contains(type);

    // Enums and every other base class library type implement IFormattable but are not identifiers.
    private static bool IsStronglyTypedId(Type type) =>
        !type.IsEnum
        && type.Assembly != typeof(object).Assembly
        && (ValuePropertyCache.GetOrAdd(type, ResolveValueProperty) is not null || typeof(IFormattable).IsAssignableFrom(type));

    // crap-exempt: single-question switch — classifies a value as one of the primitive subject-id shapes
    private static bool TryConvertPrimitive(object value, out string? result)
    {
        switch (value)
        {
            case string text:
                result = string.IsNullOrWhiteSpace(text) ? null : text;
                return true;

            case Guid guid:
                result = guid == Guid.Empty ? null : guid.ToString("D", CultureInfo.InvariantCulture);
                return true;

            case IFormattable formattable when IntegerTypes.Contains(value.GetType()):
                result = formattable.ToString(null, CultureInfo.InvariantCulture);
                return true;

            default:
                result = null;
                return false;
        }
    }

    // An empty result or an all-zero Guid still means the subject is missing.
    private static string? NormalizeFormatted(string? formatted)
    {
        if (string.IsNullOrWhiteSpace(formatted))
        {
            return null;
        }

        return Guid.TryParse(formatted, CultureInfo.InvariantCulture, out var guid) && guid == Guid.Empty
            ? null
            : formatted;
    }

    // The single public instance 'Value' property, when its type is a primitive subject type.
    private static PropertyInfo? ResolveValueProperty(Type type)
    {
        var candidates = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name == "Value" && p.GetIndexParameters().Length == 0 && p.GetMethod is not null)
            .Take(2)
            .ToList();

        // More than one public 'Value' property (for example a hidden base member) is ambiguous.
        return candidates.Count == 1 && IsPrimitiveSubjectType(Nullable.GetUnderlyingType(candidates[0].PropertyType) ?? candidates[0].PropertyType)
            ? candidates[0]
            : null;
    }
}
