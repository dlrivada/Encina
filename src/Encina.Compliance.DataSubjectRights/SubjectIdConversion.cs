using System.Globalization;
using System.Reflection;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Converts a resolved subject-id property value to a stable, invariant string identifier.
/// </summary>
/// <remarks>
/// <para>
/// A subject-id property is not always a <see cref="string"/> — <see cref="Guid"/> and numeric
/// ids are common (e.g. <c>PatientId</c>). This helper accepts any of those shapes, plus any type
/// that implements <see cref="IFormattable"/> or overrides <see cref="object.ToString()"/> (a
/// strongly-typed id wrapper), and converts the value using culture-invariant formatting.
/// </para>
/// <para>
/// A property whose value is <c>null</c> is treated as a missing subject (returns <c>null</c>),
/// not as a reason to fall back to the authenticated caller. A property whose type cannot be
/// converted at all (no <see cref="IFormattable"/> implementation and no <see cref="object.ToString()"/>
/// override) is a configuration error and throws rather than silently falling back.
/// </para>
/// <para>
/// This is intentionally a small, package-local helper rather than a shared abstraction: this
/// package, <c>Encina.Compliance.Consent</c>, and <c>Encina.Compliance.GDPR</c> do not share an
/// internal assembly, so the same handful of lines is duplicated per package instead of adding a
/// cross-package dependency purely for this helper (project history: #1149).
/// </para>
/// </remarks>
internal static class SubjectIdConversion
{
    /// <summary>
    /// Converts the value of a resolved subject-id property to its invariant string form.
    /// </summary>
    /// <param name="value">The value read from the subject-id property.</param>
    /// <param name="property">The property the value was read from, used for the error message.</param>
    /// <returns>
    /// The invariant string form of <paramref name="value"/>, or <c>null</c> if the value is
    /// <c>null</c> or an empty/whitespace string.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="value"/> is not <c>null</c> but its type cannot be converted to a stable
    /// subject identifier (not a <see cref="string"/>, <see cref="Guid"/>, <see cref="IFormattable"/>,
    /// or a type overriding <see cref="object.ToString()"/>).
    /// </exception>
    public static string? ToInvariantString(object? value, PropertyInfo property)
    {
        switch (value)
        {
            case null:
                return null;

            case string stringValue:
                return string.IsNullOrWhiteSpace(stringValue) ? null : stringValue;

            case Guid guidValue:
                return guidValue == Guid.Empty ? null : guidValue.ToString();

            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);

            default:
                if (HasCustomToString(value.GetType()))
                {
                    return value.ToString();
                }

                throw new InvalidOperationException(
                    $"Subject-id property '{property.DeclaringType?.Name}.{property.Name}' has type " +
                    $"'{value.GetType().Name}', which cannot be converted to a stable subject identifier. " +
                    "Supported types are string, Guid, numeric types, IFormattable, or a type overriding ToString().");
        }
    }

    private static bool HasCustomToString(Type type)
    {
        var toStringMethod = type.GetMethod(nameof(ToString), Type.EmptyTypes);
        return toStringMethod is not null && toStringMethod.DeclaringType != typeof(object);
    }
}
