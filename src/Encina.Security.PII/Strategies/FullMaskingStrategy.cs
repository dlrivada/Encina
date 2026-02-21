using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Default masking strategy that replaces the entire value with mask characters.
/// </summary>
/// <remarks>
/// Used as the fallback strategy for <see cref="PIIType.Custom"/> when no custom
/// regex pattern is provided, and for <see cref="Attributes.SensitiveDataAttribute"/>
/// decorated properties.
/// </remarks>
internal sealed class FullMaskingStrategy : IMaskingStrategy
{
    /// <inheritdoc />
    public string Apply(string value, MaskingOptions options)
    {
        return options.Mode switch
        {
            MaskingMode.Full => options.PreserveLength
                ? new string(options.MaskCharacter, value.Length)
                : new string(options.MaskCharacter, 3),
            MaskingMode.Redact => options.RedactedPlaceholder ?? "[REDACTED]",
            MaskingMode.Hash => HashHelper.ComputeHash(value, options.HashSalt),
            MaskingMode.Tokenize => value,
            _ => MaskPartial(value, options) // Partial
        };
    }

    private static string MaskPartial(string value, MaskingOptions options)
    {
        if (value.Length <= 2)
        {
            return new string(options.MaskCharacter, value.Length);
        }

        var visibleStart = Math.Min(options.VisibleCharactersStart, value.Length);
        var visibleEnd = Math.Min(options.VisibleCharactersEnd, value.Length - visibleStart);

        if (visibleStart + visibleEnd >= value.Length)
        {
            return value;
        }

        var maskedLength = value.Length - visibleStart - visibleEnd;
        var masked = options.PreserveLength
            ? new string(options.MaskCharacter, maskedLength)
            : new string(options.MaskCharacter, 3);

        return string.Concat(
            value.AsSpan(0, visibleStart),
            masked,
            value.AsSpan(value.Length - visibleEnd));
    }
}
