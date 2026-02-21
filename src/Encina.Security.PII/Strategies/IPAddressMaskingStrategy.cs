using Encina.Security.PII.Abstractions;

namespace Encina.Security.PII.Strategies;

/// <summary>
/// Masking strategy for IP addresses.
/// </summary>
/// <remarks>
/// <para>
/// For IPv4: preserves the first two octets (network portion) and masks the host portion.
/// Example: <c>192.168.1.100</c> becomes <c>192.168.*.*</c>.
/// </para>
/// <para>
/// For IPv6: preserves the first two groups and masks the remainder.
/// Example: <c>2001:0db8:85a3:0000:0000:8a2e:0370:7334</c> becomes <c>2001:0db8:****:****:****:****:****:****</c>.
/// </para>
/// </remarks>
internal sealed class IPAddressMaskingStrategy : IMaskingStrategy
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
        // Detect IPv6 (contains colon)
        if (value.Contains(':'))
        {
            return MaskIPv6(value, options.MaskCharacter);
        }

        // IPv4
        return MaskIPv4(value, options.MaskCharacter);
    }

    private static string MaskIPv4(string value, char maskChar)
    {
        var parts = value.Split('.');
        if (parts.Length != 4)
        {
            // Not valid IPv4 format, mask entirely
            return new string(maskChar, value.Length);
        }

        // Preserve first 2 octets, mask last 2
        var maskedOctet = new string(maskChar, 3);
        return $"{parts[0]}.{parts[1]}.{maskedOctet}.{maskedOctet}";
    }

    private static string MaskIPv6(string value, char maskChar)
    {
        var parts = value.Split(':');
        if (parts.Length < 3)
        {
            return new string(maskChar, value.Length);
        }

        // Preserve first 2 groups, mask the rest
        var maskedGroup = new string(maskChar, 4);
        var result = $"{parts[0]}:{parts[1]}";

        for (var i = 2; i < parts.Length; i++)
        {
            result += ":" + maskedGroup;
        }

        return result;
    }
}
