using Microsoft.Extensions.Options;

namespace Encina.Compliance.CrossBorderTransfer;

/// <summary>
/// Validates <see cref="CrossBorderTransferOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that cross-border transfer configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;CrossBorderTransferOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured transfer compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="CrossBorderTransferOptions.TIARiskThreshold"/> is between 0.0 and 1.0</description></item>
/// <item><description><see cref="CrossBorderTransferOptions.CacheTTLMinutes"/> is positive</description></item>
/// <item><description><see cref="CrossBorderTransferOptions.DefaultTIAExpirationDays"/> is positive when set</description></item>
/// <item><description><see cref="CrossBorderTransferOptions.DefaultSCCExpirationDays"/> is positive when set</description></item>
/// <item><description><see cref="CrossBorderTransferOptions.DefaultTransferExpirationDays"/> is positive when set</description></item>
/// <item><description><see cref="CrossBorderTransferOptions.DefaultSourceCountryCode"/> is not empty</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class CrossBorderTransferOptionsValidator : IValidateOptions<CrossBorderTransferOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CrossBorderTransferOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate TIARiskThreshold
        if (options.TIARiskThreshold is < 0.0 or > 1.0)
        {
            failures.Add(
                $"CrossBorderTransferOptions.TIARiskThreshold must be between 0.0 and 1.0 (current: {options.TIARiskThreshold}). "
                + "This threshold determines when a TIA risk score requires supplementary measures.");
        }

        // Validate CacheTTLMinutes
        if (options.CacheTTLMinutes <= 0)
        {
            failures.Add(
                $"CrossBorderTransferOptions.CacheTTLMinutes must be a positive number (current: {options.CacheTTLMinutes}). "
                + "Cache TTL controls how long transfer validation results are cached.");
        }

        // Validate DefaultTIAExpirationDays
        if (options.DefaultTIAExpirationDays is not null and <= 0)
        {
            failures.Add(
                $"CrossBorderTransferOptions.DefaultTIAExpirationDays must be a positive number when set (current: {options.DefaultTIAExpirationDays}). "
                + "TIA assessments need a meaningful expiration period.");
        }

        // Validate DefaultSCCExpirationDays
        if (options.DefaultSCCExpirationDays is not null and <= 0)
        {
            failures.Add(
                $"CrossBorderTransferOptions.DefaultSCCExpirationDays must be a positive number when set (current: {options.DefaultSCCExpirationDays}). "
                + "SCC agreements need a meaningful expiration period.");
        }

        // Validate DefaultTransferExpirationDays
        if (options.DefaultTransferExpirationDays is not null and <= 0)
        {
            failures.Add(
                $"CrossBorderTransferOptions.DefaultTransferExpirationDays must be a positive number when set (current: {options.DefaultTransferExpirationDays}). "
                + "Transfer authorizations need a meaningful expiration period.");
        }

        // Validate DefaultSourceCountryCode
        if (string.IsNullOrWhiteSpace(options.DefaultSourceCountryCode))
        {
            failures.Add(
                "CrossBorderTransferOptions.DefaultSourceCountryCode must not be empty. "
                + "Set this to the ISO 3166-1 alpha-2 code of the country where your primary data center is located (e.g., \"DE\").");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
