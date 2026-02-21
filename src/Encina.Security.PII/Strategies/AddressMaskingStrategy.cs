using System.Text;
using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for physical addresses.
/// </summary>
/// <remarks>
/// <para>
/// Masks the street-level details (number, street name) while preserving
/// city-level information for general geographic context.
/// Example: <c>123 Main St, Springfield, IL</c> becomes <c>*** **** **, Springfield, IL</c>.
/// </para>
/// <para>
/// Uses comma as the primary delimiter: content before the first comma is considered
/// street-level detail and is masked; content after is preserved.
/// </para>
/// </remarks>
internal sealed class AddressMaskingStrategy : IMaskingStrategy
{
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
        var commaIndex = value.IndexOf(',');
        if (commaIndex < 0)
        {
            // No comma - mask entire value preserving word structure
            return MaskWords(value, options.MaskCharacter);
        }

        // Mask street portion (before first comma), preserve the rest
        var streetPortion = value[..commaIndex];
        var remainder = value[commaIndex..]; // includes comma

        return MaskWords(streetPortion, options.MaskCharacter) + remainder;
    }

    private static string MaskWords(string text, char maskChar)
    {
        var result = new StringBuilder(text.Length);

        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsLetterOrDigit(text[i]))
            {
                result.Append(maskChar);
            }
            else
            {
                result.Append(text[i]); // Preserve spaces, punctuation
            }
        }

        return result.ToString();
    }
}
