using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.Techniques;

/// <summary>
/// Anonymization technique that reduces precision by replacing values with broader categories or ranges.
/// </summary>
/// <remarks>
/// <para>
/// Generalization preserves some data utility while reducing the ability to identify individuals.
/// For numeric values, it replaces exact values with ranges (e.g., age 34 → "30-39").
/// For date values, it truncates to a less precise level (e.g., date → month or year).
/// For string values, it truncates or replaces trailing characters with wildcards.
/// </para>
/// <para>
/// Technique-specific parameters:
/// <list type="bullet">
/// <item>
/// <term><c>"Granularity"</c></term>
/// <description>
/// Controls the range size for numeric generalization or the truncation level.
/// For numeric types: the range width (e.g., 10 groups ages into decades).
/// For dates: 1 = year, 2 = month, 3 = day (default: 1 = year).
/// For strings: number of characters to preserve from the start (default: 3).
/// </description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class GeneralizationTechnique : IAnonymizationTechnique
{
    private const int DefaultGranularity = 10;
    private const int DefaultDateGranularity = 1; // Year
    private const int DefaultStringPreserveLength = 3;

    /// <inheritdoc/>
    public AnonymizationTechnique Technique => AnonymizationTechnique.Generalization;

    /// <inheritdoc/>
    /// <remarks>
    /// Generalization supports numeric types (<c>int</c>, <c>long</c>, <c>double</c>, <c>decimal</c>, etc.),
    /// <c>DateTime</c>, <c>DateTimeOffset</c>, <c>DateOnly</c>, and <c>string</c>.
    /// </remarks>
    public bool CanApply(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;

        return underlying == typeof(string)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(DateOnly)
            || IsNumericType(underlying);
    }

    /// <inheritdoc/>
    public ValueTask<Either<EncinaError, object?>> ApplyAsync(
        object? value,
        Type valueType,
        IReadOnlyDictionary<string, object>? parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        if (value is null)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(null));
        }

        try
        {
            var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;
            var granularity = GetGranularity(parameters);

            object? result;

            if (IsNumericType(underlying))
            {
                result = GeneralizeNumeric(value, granularity);
            }
            else if (underlying == typeof(DateTime))
            {
                result = GeneralizeDateTime((DateTime)value, granularity);
            }
            else if (underlying == typeof(DateTimeOffset))
            {
                result = GeneralizeDateTimeOffset((DateTimeOffset)value, granularity);
            }
            else if (underlying == typeof(DateOnly))
            {
                result = GeneralizeDateOnly((DateOnly)value, granularity);
            }
            else if (underlying == typeof(string))
            {
                var preserveLength = parameters is not null
                    && parameters.TryGetValue("Granularity", out var gl)
                    ? Convert.ToInt32(gl, System.Globalization.CultureInfo.InvariantCulture)
                    : DefaultStringPreserveLength;
                result = GeneralizeString((string)value, preserveLength);
            }
            else
            {
                return ValueTask.FromResult<Either<EncinaError, object?>>(
                    Left<EncinaError, object?>(
                        AnonymizationErrors.TechniqueNotApplicable(
                            AnonymizationTechnique.Generalization, "(field)", valueType)));
            }

            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.AnonymizationFailed("(generalization)", ex.Message, ex)));
        }
    }

    private static int GetGranularity(IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters is not null && parameters.TryGetValue("Granularity", out var granularityObj))
        {
            return Convert.ToInt32(granularityObj, System.Globalization.CultureInfo.InvariantCulture);
        }

        return DefaultGranularity;
    }

    private static string GeneralizeNumeric(object value, int granularity)
    {
        var numericValue = Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        var lowerBound = Math.Floor(numericValue / granularity) * granularity;
        var upperBound = lowerBound + granularity - 1;
        return $"{lowerBound:F0}-{upperBound:F0}";
    }

    private static DateTime GeneralizeDateTime(DateTime value, int granularity) =>
        granularity switch
        {
            >= 3 => new DateTime(value.Year, value.Month, value.Day, 0, 0, 0, value.Kind),
            2 => new DateTime(value.Year, value.Month, 1, 0, 0, 0, value.Kind),
            _ => new DateTime(value.Year, 1, 1, 0, 0, 0, value.Kind)
        };

    private static DateTimeOffset GeneralizeDateTimeOffset(DateTimeOffset value, int granularity) =>
        granularity switch
        {
            >= 3 => new DateTimeOffset(value.Year, value.Month, value.Day, 0, 0, 0, value.Offset),
            2 => new DateTimeOffset(value.Year, value.Month, 1, 0, 0, 0, value.Offset),
            _ => new DateTimeOffset(value.Year, 1, 1, 0, 0, 0, value.Offset)
        };

    private static DateOnly GeneralizeDateOnly(DateOnly value, int granularity) =>
        granularity switch
        {
            >= 3 => value,
            2 => new DateOnly(value.Year, value.Month, 1),
            _ => new DateOnly(value.Year, 1, 1)
        };

    private static string GeneralizeString(string value, int preserveLength)
    {
        if (value.Length <= preserveLength)
        {
            return value;
        }

        var preserved = value[..preserveLength];
        var masked = new string('*', value.Length - preserveLength);
        return preserved + masked;
    }

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte)
        || type == typeof(short) || type == typeof(ushort)
        || type == typeof(int) || type == typeof(uint)
        || type == typeof(long) || type == typeof(ulong)
        || type == typeof(float) || type == typeof(double)
        || type == typeof(decimal);
}
