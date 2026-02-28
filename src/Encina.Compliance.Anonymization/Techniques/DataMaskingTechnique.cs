using Encina.Compliance.Anonymization.Model;

using LanguageExt;

using static LanguageExt.Prelude;

namespace Encina.Compliance.Anonymization.Techniques;

/// <summary>
/// Anonymization technique that partially masks values while preserving format and a recognizable portion.
/// </summary>
/// <remarks>
/// <para>
/// Data masking replaces sensitive characters with a mask character (default: <c>*</c>) while
/// preserving a portion of the original value for human verification. This is useful when
/// partial visibility is needed without full disclosure (e.g., displaying last four digits
/// of a credit card, or preserving the email domain).
/// </para>
/// <para>
/// Technique-specific parameters:
/// <list type="bullet">
/// <item>
/// <term><c>"MaskChar"</c></term>
/// <description>
/// The character used for masking. Default: <c>'*'</c>.
/// </description>
/// </item>
/// <item>
/// <term><c>"PreserveStart"</c></term>
/// <description>
/// Number of characters to preserve at the start of the value. Default: <c>1</c>.
/// </description>
/// </item>
/// <item>
/// <term><c>"PreserveEnd"</c></term>
/// <description>
/// Number of characters to preserve at the end of the value. Default: <c>0</c>.
/// </description>
/// </item>
/// <item>
/// <term><c>"PreserveDomain"</c></term>
/// <description>
/// When <c>true</c>, preserves the domain portion of email addresses (text after <c>@</c>).
/// Default: <c>false</c>.
/// </description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class DataMaskingTechnique : IAnonymizationTechnique
{
    private const char DefaultMaskChar = '*';
    private const int DefaultPreserveStart = 1;
    private const int DefaultPreserveEnd = 0;

    /// <inheritdoc/>
    public AnonymizationTechnique Technique => AnonymizationTechnique.DataMasking;

    /// <inheritdoc/>
    /// <remarks>
    /// Data masking supports <c>string</c> values only. Numeric and date values
    /// should use <see cref="AnonymizationTechnique.Generalization"/> or
    /// <see cref="AnonymizationTechnique.Perturbation"/> instead.
    /// </remarks>
    public bool CanApply(Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);

        var underlying = Nullable.GetUnderlyingType(valueType) ?? valueType;
        return underlying == typeof(string);
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

        if (value is not string stringValue)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.TechniqueNotApplicable(
                        AnonymizationTechnique.DataMasking, "(field)", valueType)));
        }

        try
        {
            var maskChar = GetMaskChar(parameters);
            var preserveDomain = GetBoolParameter(parameters, "PreserveDomain");

            string result;

            if (preserveDomain && stringValue.Contains('@'))
            {
                result = MaskEmail(stringValue, maskChar, parameters);
            }
            else
            {
                var preserveStart = GetIntParameter(parameters, "PreserveStart", DefaultPreserveStart);
                var preserveEnd = GetIntParameter(parameters, "PreserveEnd", DefaultPreserveEnd);
                result = MaskString(stringValue, maskChar, preserveStart, preserveEnd);
            }

            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Right<EncinaError, object?>(result));
        }
        catch (Exception ex)
        {
            return ValueTask.FromResult<Either<EncinaError, object?>>(
                Left<EncinaError, object?>(
                    AnonymizationErrors.AnonymizationFailed("(data-masking)", ex.Message, ex)));
        }
    }

    private static string MaskString(string value, char maskChar, int preserveStart, int preserveEnd)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var totalPreserved = preserveStart + preserveEnd;
        if (totalPreserved >= value.Length)
        {
            return value;
        }

        var maskedLength = value.Length - totalPreserved;
        var startPart = value[..preserveStart];
        var endPart = preserveEnd > 0 ? value[^preserveEnd..] : string.Empty;
        var maskedPart = new string(maskChar, maskedLength);

        return startPart + maskedPart + endPart;
    }

    private static string MaskEmail(string email, char maskChar, IReadOnlyDictionary<string, object>? parameters)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex < 0)
        {
            // Not a valid email format, fall back to standard masking
            var ps = GetIntParameter(parameters, "PreserveStart", DefaultPreserveStart);
            var pe = GetIntParameter(parameters, "PreserveEnd", DefaultPreserveEnd);
            return MaskString(email, maskChar, ps, pe);
        }

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..]; // includes @

        var preserveStart = GetIntParameter(parameters, "PreserveStart", DefaultPreserveStart);

        // Mask the local part, preserve domain
        var maskedLocal = localPart.Length <= preserveStart
            ? localPart
            : localPart[..preserveStart] + new string(maskChar, localPart.Length - preserveStart);

        return maskedLocal + domainPart;
    }

    private static char GetMaskChar(IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters is not null && parameters.TryGetValue("MaskChar", out var maskCharObj))
        {
            var str = maskCharObj.ToString();
            return string.IsNullOrEmpty(str) ? DefaultMaskChar : str[0];
        }

        return DefaultMaskChar;
    }

    private static int GetIntParameter(IReadOnlyDictionary<string, object>? parameters, string key, int defaultValue)
    {
        if (parameters is not null && parameters.TryGetValue(key, out var obj))
        {
            return Convert.ToInt32(obj, System.Globalization.CultureInfo.InvariantCulture);
        }

        return defaultValue;
    }

    private static bool GetBoolParameter(IReadOnlyDictionary<string, object>? parameters, string key)
    {
        if (parameters is not null && parameters.TryGetValue(key, out var obj))
        {
            return Convert.ToBoolean(obj, System.Globalization.CultureInfo.InvariantCulture);
        }

        return false;
    }
}
