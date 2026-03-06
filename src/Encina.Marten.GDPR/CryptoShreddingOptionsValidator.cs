using Microsoft.Extensions.Options;

namespace Encina.Marten.GDPR;

/// <summary>
/// Validates <see cref="CryptoShreddingOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// This validator runs during the first <c>IOptions&lt;CryptoShreddingOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured crypto-shredding settings.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="CryptoShreddingOptions.KeyRotationDays"/> is positive</description></item>
/// <item><description><see cref="CryptoShreddingOptions.AnonymizedPlaceholder"/> is not null or empty</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class CryptoShreddingOptionsValidator : IValidateOptions<CryptoShreddingOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CryptoShreddingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.KeyRotationDays <= 0)
        {
            failures.Add(
                "CryptoShreddingOptions.KeyRotationDays must be a positive number. "
                + "Recommended value is 90 days for regular key rotation.");
        }

        if (string.IsNullOrWhiteSpace(options.AnonymizedPlaceholder))
        {
            failures.Add(
                "CryptoShreddingOptions.AnonymizedPlaceholder must not be null or empty. "
                + "This value replaces PII when a data subject has been cryptographically forgotten.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
