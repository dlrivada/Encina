using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for email addresses.
/// </summary>
/// <remarks>
/// <para>
/// Preserves the first character of the local part and the full domain.
/// Example: <c>john.doe@example.com</c> becomes <c>j***@example.com</c>.
/// </para>
/// <para>
/// When <see cref="MaskingMode.Full"/> is used, the entire local part is replaced:
/// <c>***@example.com</c>.
/// </para>
/// </remarks>
internal sealed class EmailMaskingStrategy : IMaskingStrategy
{
    /// <inheritdoc />
    public string Apply(string value, MaskingOptions options)
    {
        var atIndex = value.IndexOf('@');
        if (atIndex < 0)
        {
            // Not a valid email format, apply generic masking
            return ApplyGenericMask(value, options);
        }

        var localPart = value[..atIndex];
        var domain = value[atIndex..]; // includes @

        return options.Mode switch
        {
            MaskingMode.Full => new string(options.MaskCharacter, 3) + domain,
            MaskingMode.Redact => options.RedactedPlaceholder ?? "[REDACTED]",
            MaskingMode.Hash => HashHelper.ComputeHash(value, options.HashSalt),
            MaskingMode.Tokenize => value, // Tokenization requires external vault, pass-through
            _ => MaskLocalPart(localPart, domain, options) // Partial
        };
    }

    private static string MaskLocalPart(string localPart, string domain, MaskingOptions options)
    {
        if (localPart.Length <= 1)
        {
            return new string(options.MaskCharacter, 3) + domain;
        }

        var visibleStart = Math.Min(options.VisibleCharactersStart > 0 ? options.VisibleCharactersStart : 1, localPart.Length);
        var masked = localPart[..visibleStart] + new string(options.MaskCharacter, 3);
        return masked + domain;
    }

    private static string ApplyGenericMask(string value, MaskingOptions options)
    {
        if (value.Length <= 2)
        {
            return new string(options.MaskCharacter, value.Length);
        }

        return value[..1] + new string(options.MaskCharacter, value.Length - 1);
    }
}
