using System.Text;
using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for person names.
/// </summary>
/// <remarks>
/// <para>
/// Preserves the first character of each name part and masks the remainder.
/// Example: <c>John Doe</c> becomes <c>J*** D**</c>.
/// </para>
/// <para>
/// Handles multi-part names separated by spaces, hyphens, or periods.
/// </para>
/// </remarks>
internal sealed class NameMaskingStrategy : IMaskingStrategy
{
    private static readonly char[] NameSeparators = [' ', '-', '.'];

    /// <inheritdoc />
    public string Apply(string value, MaskingOptions options)
    {
        return options.Mode switch
        {
            MaskingMode.Full => new string(options.MaskCharacter, value.Length),
            MaskingMode.Redact => options.RedactedPlaceholder ?? "[REDACTED]",
            MaskingMode.Hash => HashHelper.ComputeHash(value, options.HashSalt),
            MaskingMode.Tokenize => value,
            _ => MaskPartial(value, options) // Partial
        };
    }

    private static string MaskPartial(string value, MaskingOptions options)
    {
        var parts = value.Split(NameSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return value;
        }

        // Find separators to preserve them
        var result = new StringBuilder(value.Length);
        var currentIndex = 0;

        foreach (var part in parts)
        {
            // Append any separators before this part
            var partIndex = value.IndexOf(part, currentIndex, StringComparison.Ordinal);
            if (partIndex > currentIndex)
            {
                result.Append(value, currentIndex, partIndex - currentIndex);
            }

            // Mask the name part: keep first character, mask the rest
            if (part.Length <= 1)
            {
                result.Append(part);
            }
            else
            {
                var visibleStart = Math.Min(
                    options.VisibleCharactersStart > 0 ? options.VisibleCharactersStart : 1,
                    part.Length);
                result.Append(part[..visibleStart]);
                result.Append(options.MaskCharacter, part.Length - visibleStart);
            }

            currentIndex = partIndex + part.Length;
        }

        // Append any trailing separators
        if (currentIndex < value.Length)
        {
            result.Append(value, currentIndex, value.Length - currentIndex);
        }

        return result.ToString();
    }
}
