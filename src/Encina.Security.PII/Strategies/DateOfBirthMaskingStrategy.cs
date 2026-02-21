using System.Text;
using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for dates of birth.
/// </summary>
/// <remarks>
/// <para>
/// Preserves the year while masking the month and day.
/// Example: <c>03/15/1990</c> becomes <c>**/**/1990</c>.
/// </para>
/// <para>
/// Supports common date formats with <c>/</c>, <c>-</c>, or <c>.</c> separators,
/// as well as ISO 8601 format (<c>1990-03-15</c>).
/// </para>
/// </remarks>
internal sealed class DateOfBirthMaskingStrategy : IMaskingStrategy
{
    private static readonly char[] DateSeparators = ['/', '-', '.'];

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
        var parts = value.Split(DateSeparators);
        if (parts.Length < 3)
        {
            // Cannot determine date structure, mask all digits
            return MaskAllDigits(value, options.MaskCharacter);
        }

        // Find the year part (the one with 4 digits, or if all have 2, the last one)
        var yearIndex = FindYearPartIndex(parts);

        // Rebuild with separators preserved
        var result = new StringBuilder(value.Length);
        var currentPos = 0;

        for (var i = 0; i < parts.Length; i++)
        {
            // Append separator between parts
            if (i > 0)
            {
                var sepPos = currentPos;
                while (sepPos < value.Length && !DateSeparators.Contains(value[sepPos]))
                {
                    sepPos++;
                }

                if (sepPos < value.Length)
                {
                    result.Append(value[sepPos]);
                    currentPos = sepPos + 1;
                }
            }

            // Skip to the start of this part
            while (currentPos < value.Length && DateSeparators.Contains(value[currentPos]))
            {
                currentPos++;
            }

            if (i == yearIndex)
            {
                // Preserve year
                result.Append(parts[i]);
            }
            else
            {
                // Mask month/day
                result.Append(new string(options.MaskCharacter, parts[i].Length));
            }

            currentPos += parts[i].Length;
        }

        return result.ToString();
    }

    private static int FindYearPartIndex(string[] parts)
    {
        // First, look for a 4-digit part (unambiguous year)
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 4 && parts[i].All(char.IsDigit))
            {
                return i;
            }
        }

        // If ISO format (YYYY-MM-DD), year is first
        if (parts[0].Length >= 4)
        {
            return 0;
        }

        // Default: assume year is last (MM/DD/YYYY or DD/MM/YYYY)
        return parts.Length - 1;
    }

    private static string MaskAllDigits(string value, char maskChar)
    {
        return string.Create(value.Length, (value, maskChar), static (span, state) =>
        {
            for (var i = 0; i < state.value.Length; i++)
            {
                span[i] = char.IsDigit(state.value[i]) ? state.maskChar : state.value[i];
            }
        });
    }
}
