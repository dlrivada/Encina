using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for credit card numbers.
/// </summary>
/// <remarks>
/// <para>
/// Reveals only the last four digits, following PCI-DSS guidelines for
/// acceptable display of primary account numbers (PAN).
/// Example: <c>4532-1234-5678-9012</c> becomes <c>****-****-****-9012</c>.
/// </para>
/// </remarks>
internal sealed class CreditCardMaskingStrategy : IMaskingStrategy
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

        var digits = value.Where(char.IsDigit).ToArray();
        if (digits.Length <= visibleEnd)
        {
            return value; // Too short to mask meaningfully
        }

        // Count digits from the end to determine which to keep visible
        var digitCount = 0;
        var totalDigits = digits.Length;
        var visibleDigitPositions = new HashSet<int>();

        for (var i = value.Length - 1; i >= 0 && digitCount < visibleEnd; i--)
        {
            if (char.IsDigit(value[i]))
            {
                visibleDigitPositions.Add(i);
                digitCount++;
            }
        }

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
                    span[i] = state.value[i];
                }
            }
        });
    }
}
