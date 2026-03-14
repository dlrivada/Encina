using Microsoft.Extensions.Options;

namespace Encina.Compliance.PrivacyByDesign;

/// <summary>
/// Validates <see cref="PrivacyByDesignOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that Privacy by Design configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;PrivacyByDesignOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured Privacy by Design compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="PrivacyByDesignOptions.EnforcementMode"/> is a defined enum value</description></item>
/// <item><description><see cref="PrivacyByDesignOptions.PrivacyLevel"/> is a defined enum value</description></item>
/// <item><description><see cref="PrivacyByDesignOptions.MinimizationScoreThreshold"/> is in the valid range [0.0, 1.0]</description></item>
/// <item><description>Purpose builders have required fields (Description, LegalBasis)</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class PrivacyByDesignOptionsValidator : IValidateOptions<PrivacyByDesignOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PrivacyByDesignOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate EnforcementMode is a defined enum value
        if (!Enum.IsDefined(options.EnforcementMode))
        {
            failures.Add(
                $"PrivacyByDesignOptions.EnforcementMode has an invalid value '{(int)options.EnforcementMode}'. "
                + "Valid values are Disabled (0), Warn (1), or Block (2).");
        }

        // Validate PrivacyLevel is a defined enum value
        if (!Enum.IsDefined(options.PrivacyLevel))
        {
            failures.Add(
                $"PrivacyByDesignOptions.PrivacyLevel has an invalid value '{(int)options.PrivacyLevel}'. "
                + "Valid values are Minimum (0), Standard (1), or Maximum (2).");
        }

        // Validate MinimizationScoreThreshold is in [0.0, 1.0]
        if (options.MinimizationScoreThreshold is < 0.0 or > 1.0)
        {
            failures.Add(
                $"PrivacyByDesignOptions.MinimizationScoreThreshold must be between 0.0 and 1.0. "
                + $"Current value: {options.MinimizationScoreThreshold}.");
        }

        // Validate purpose builders
        for (var i = 0; i < options.PurposeBuilders.Count; i++)
        {
            var builder = options.PurposeBuilders[i];

            if (string.IsNullOrWhiteSpace(builder.Description))
            {
                failures.Add(
                    $"Purpose '{builder.Name}' (index {i}) must have a Description. "
                    + "Per GDPR Article 5(1)(b), purposes must be explicit.");
            }

            if (string.IsNullOrWhiteSpace(builder.LegalBasis))
            {
                failures.Add(
                    $"Purpose '{builder.Name}' (index {i}) must have a LegalBasis. "
                    + "Per GDPR Article 6(1), processing requires a legal basis.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
