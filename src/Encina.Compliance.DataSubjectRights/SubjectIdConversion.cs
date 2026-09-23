using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Converts the value of a resolved subject-id property to a stable, culture-invariant string.
/// </summary>
/// <remarks>
/// <para>
/// Supported id shapes:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="string"/> — used as is.</description></item>
/// <item><description><see cref="Guid"/> — formatted with the <c>"D"</c> format.</description></item>
/// <item><description>Integer types (<see cref="sbyte"/>, <see cref="byte"/>, <see cref="short"/>,
/// <see cref="ushort"/>, <see cref="int"/>, <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>,
/// <see cref="Int128"/>, <see cref="UInt128"/>) — formatted with the invariant culture. <c>0</c> is a
/// valid id, not a missing subject.</description></item>
/// <item><description>Strongly-typed ids declared outside the base class library that implement
/// <see cref="IFormattable"/> (or <see cref="ISpanFormattable"/>) — formatted with
/// <c>ToString(null, CultureInfo.InvariantCulture)</c>.</description></item>
/// <item><description>Strongly-typed id wrappers (record struct, record class or plain struct/class)
/// that expose a public instance <c>Value</c> property of one of the primitive types above — the
/// <c>Value</c> is unwrapped and converted. The compiler-generated record <c>ToString()</c>
/// (<c>PatientId { Value = ... }</c>) and <see cref="ValueType.ToString()"/> (the type name) are
/// never used.</description></item>
/// </list>
/// <para>
/// <c>null</c>, <see cref="Guid.Empty"/>, and an empty or whitespace string (including when unwrapped
/// from a <c>Value</c> property) mean "subject missing" and return <c>null</c>. A wrapper's primitive
/// <c>Value</c> is unwrapped before its <see cref="IFormattable"/> implementation is considered, and an
/// <see cref="IFormattable"/> id that formats as an all-zero Guid is missing too. Any other type — for
/// example <see cref="double"/>, <see cref="DateTime"/>, an enum, or a wrapper without a supported
/// <c>Value</c> property — is a configuration error and throws <see cref="InvalidOperationException"/>
/// rather than silently producing an unstable identifier or falling back to the authenticated caller.
/// </para>
/// <para>
/// <c>Encina.Compliance.DataSubjectRights</c> and <c>Encina.Compliance.Consent</c> share no internal
/// assembly, so this helper exists as two identical copies (one per package, in the package's root
/// namespace). A unit test asserts that both copies behave identically on the same inputs; change
/// them together (project history: #1149).
/// </para>
/// </remarks>
internal static class SubjectIdConversion
{
    private static readonly System.Collections.Generic.HashSet<Type> IntegerTypes =
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

    /// <summary>
    /// Converts the value of a resolved subject-id property to its invariant string form.
    /// </summary>
    /// <param name="value">The value read from the subject-id property.</param>
    /// <param name="property">The property the value was read from, used for the error message.</param>
    /// <returns>
    /// The invariant string form of <paramref name="value"/>, or <c>null</c> when the subject is
    /// missing (<c>null</c>, <see cref="Guid.Empty"/>, or an empty/whitespace string).
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value"/> is not <c>null</c> but its type is not a supported subject-id shape.
    /// </exception>
    public static string? ToInvariantString(object? value, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property);

        if (value is null)
        {
            return null;
        }

        if (TryConvertPrimitive(value, out var primitive))
        {
            return primitive;
        }

        var type = value.GetType();

        // Enums and every other base-class-library type (double, decimal, DateTime, TimeSpan, ...)
        // implement IFormattable but are not identifiers.
        if (!type.IsEnum && type.Assembly != typeof(object).Assembly)
        {
            // Unwrap a primitive 'Value' first, even when the wrapper also implements IFormattable:
            // the wrapped value decides whether the subject is missing (null, Guid.Empty, empty string).
            var valueProperty = ValuePropertyCache.GetOrAdd(type, ResolveValueProperty);
            if (valueProperty is not null)
            {
                var inner = valueProperty.GetValue(value);
                if (inner is null)
                {
                    return null;
                }

                if (TryConvertPrimitive(inner, out var unwrapped))
                {
                    return unwrapped;
                }
            }

            if (value is IFormattable formattable)
            {
                return NormalizeFormatted(formattable.ToString(null, CultureInfo.InvariantCulture));
            }
        }

        throw new InvalidOperationException(
            $"Subject-id property '{property.DeclaringType?.Name}.{property.Name}' has type '{type.Name}', " +
            "which is not a supported subject identifier. Supported types are string, Guid, integer types, " +
            "strongly-typed ids implementing IFormattable, and wrappers exposing a public 'Value' property of " +
            "one of those primitive types.");
    }

    private static bool TryConvertPrimitive(object value, out string? result)
    {
        switch (value)
        {
            case string stringValue:
                result = string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;
                return true;

            case Guid guidValue:
                result = guidValue == Guid.Empty ? null : guidValue.ToString("D", CultureInfo.InvariantCulture);
                return true;

            case IFormattable formattable when IntegerTypes.Contains(value.GetType()):
                result = formattable.ToString(null, CultureInfo.InvariantCulture);
                return true;

            default:
                result = null;
                return false;
        }
    }

    // A strongly-typed id without a readable 'Value' is formatted as is; an empty result or an all-zero
    // Guid still means the subject is missing.
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

    private static PropertyInfo? ResolveValueProperty(Type type)
    {
        PropertyInfo? match = null;

        foreach (var candidate in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (candidate.Name != "Value" || candidate.GetIndexParameters().Length != 0 || candidate.GetMethod is null)
            {
                continue;
            }

            if (match is not null)
            {
                // More than one public 'Value' property (e.g. a hidden base member) is ambiguous.
                return null;
            }

            match = candidate;
        }

        if (match is null)
        {
            return null;
        }

        var valueType = Nullable.GetUnderlyingType(match.PropertyType) ?? match.PropertyType;
        var supported = valueType == typeof(string) || valueType == typeof(Guid) || IntegerTypes.Contains(valueType);
        return supported ? match : null;
    }
}
