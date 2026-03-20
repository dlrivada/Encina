using Encina.Compliance.NIS2.Model;

using Microsoft.Extensions.Options;

namespace Encina.Compliance.NIS2;

/// <summary>
/// Validates <see cref="NIS2Options"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that NIS2 compliance configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;NIS2Options&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured NIS2 compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="NIS2Options.EnforcementMode"/> is a defined enum value.</description></item>
/// <item><description><see cref="NIS2Options.EntityType"/> is a defined enum value.</description></item>
/// <item><description><see cref="NIS2Options.IncidentNotificationHours"/> is positive.</description></item>
/// <item><description><see cref="NIS2Options.CompetentAuthority"/> is set when <see cref="NIS2Options.EntityType"/>
///   is <see cref="NIS2EntityType.Essential"/> (stricter reporting obligations).</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class NIS2OptionsValidator : IValidateOptions<NIS2Options>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, NIS2Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"NIS2Options.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Block (0), Warn (1), or Disabled (2).");
        }

        // Validate EntityType is a defined enum value
        if (!Enum.IsDefined(options.EntityType))
        {
            failures.Add(
                $"NIS2Options.EntityType has an invalid value '{(int)options.EntityType}'. "
                + "Valid values are Essential (0) or Important (1).");
        }

        // Validate Sector is a defined enum value
        if (!Enum.IsDefined(options.Sector))
        {
            failures.Add(
                $"NIS2Options.Sector has an invalid value '{(int)options.Sector}'. "
                + "See NIS2Sector enum for valid values (Annexes I and II).");
        }

        // Validate IncidentNotificationHours is positive
        if (options.IncidentNotificationHours <= 0)
        {
            failures.Add(
                $"NIS2Options.IncidentNotificationHours must be positive. "
                + $"Current value: {options.IncidentNotificationHours}. "
                + "Per Art. 23(4)(a), the early warning deadline is 24 hours.");
        }

        // Essential entities have stricter obligations — CompetentAuthority should be configured
        if (options.EntityType == NIS2EntityType.Essential
            && string.IsNullOrWhiteSpace(options.CompetentAuthority))
        {
            failures.Add(
                "NIS2Options.CompetentAuthority must be configured for Essential entities. "
                + "Per Art. 23(1), essential entities must notify their CSIRT or competent authority "
                + "of any significant incident. Set CompetentAuthority to the authority's contact endpoint.");
        }

        // Validate ComplianceCacheTTL is non-negative
        if (options.ComplianceCacheTTL < TimeSpan.Zero)
        {
            failures.Add(
                $"NIS2Options.ComplianceCacheTTL cannot be negative. "
                + $"Current value: {options.ComplianceCacheTTL}. "
                + "Set to TimeSpan.Zero to disable caching, or a positive value to enable.");
        }

        // Validate ExternalCallTimeout is positive
        if (options.ExternalCallTimeout <= TimeSpan.Zero)
        {
            failures.Add(
                $"NIS2Options.ExternalCallTimeout must be positive. "
                + $"Current value: {options.ExternalCallTimeout}. "
                + "This timeout protects external calls (IKeyProvider, ICacheProvider, etc.) from blocking indefinitely.");
        }

        // Validate encryption coherence: EnforceEncryption requires at least some encrypted categories or endpoints
        if (options.EnforceEncryption
            && options.EncryptedDataCategories.Count == 0
            && options.EncryptedEndpoints.Count == 0)
        {
            failures.Add(
                "NIS2Options.EnforceEncryption is true but no EncryptedDataCategories or EncryptedEndpoints "
                + "are configured. The encryption pipeline behavior will reject requests that reference "
                + "unregistered categories/endpoints. Add at least one category or endpoint, "
                + "or set EnforceEncryption to false.");
        }

        // Validate supplier configurations
        foreach (var (supplierId, config) in options.Suppliers)
        {
            if (string.IsNullOrWhiteSpace(config.Name))
            {
                failures.Add(
                    $"Supplier '{supplierId}' has no name configured. "
                    + "Set the Name property in AddSupplier() configuration.");
            }

            if (!Enum.IsDefined(config.RiskLevel))
            {
                failures.Add(
                    $"Supplier '{supplierId}' has an invalid RiskLevel value '{(int)config.RiskLevel}'. "
                    + "Valid values are Low (0), Medium (1), High (2), or Critical (3).");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
