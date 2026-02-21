using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for phone numbers.
/// </summary>
/// <remarks>
/// <para>
/// Preserves the last four digits for partial identification.
/// Example: <c>+1-555-123-4567</c> becomes <c>***-***-4567</c>.
/// </para>
/// <para>
/// Non-digit characters (dashes, spaces, parentheses) are preserved in the masked output
/// to maintain the original formatting pattern.
/// </para>
/// </remarks>
internal sealed class PhoneMaskingStrategy : IMaskingStrategy
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
        var visibleEnd = options.VisibleCharactersEnd > 0 ? options.VisibleCharactersEnd : 4;

        // Extract only digits
        var digits = value.Where(char.IsDigit).ToArray();
        if (digits.Length <= visibleEnd)
        {
            return value; // Too short to mask meaningfully
        }

        // Build a set of digit positions to keep visible (from the end)
        var digitCount = 0;
        var totalDigits = digits.Length;
        var visibleDigitPositions = new HashSet<int>();

        // Mark visible digits from the end
        var digitIndex = totalDigits - 1;
        for (var i = value.Length - 1; i >= 0 && digitCount < visibleEnd; i--)
        {
            if (char.IsDigit(value[i]))
            {
                visibleDigitPositions.Add(i);
                digitCount++;
            }
        }

        // Build masked output preserving formatting characters
        return string.Create(value.Length, (value, options.MaskCharacter, visibleDigitPositions), static (span, state) =>
        {
            for (var i = 0; i < state.value.Length; i++)
            {
                if (char.IsDigit(state.value[i]))
                {
                    span[i] = state.visibleDigitPositions.Contains(i) ? state.value[i] : state.MaskCharacter;
                }
                else
                {
                    // Preserve formatting characters (dashes, spaces, parentheses, plus)
                    span[i] = state.value[i];
                }
            }
        });
    }
}
