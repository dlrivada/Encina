using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for Social Security Numbers (SSN) and national identification numbers.
/// </summary>
/// <remarks>
/// <para>
/// Reveals only the last four digits for partial identification.
/// Example: <c>123-45-6789</c> becomes <c>***-**-6789</c>.
/// </para>
/// </remarks>
internal sealed class SSNMaskingStrategy : IMaskingStrategy
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
            return value;
        }

        var digitCount = 0;
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
