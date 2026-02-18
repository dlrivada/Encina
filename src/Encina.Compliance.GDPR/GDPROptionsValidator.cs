using Microsoft.Extensions.Options;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Validates <see cref="GDPROptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that required fields are populated when enforcement is enabled.
/// This validator runs during the first <c>IOptions&lt;GDPROptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured GDPR compliance.
/// </para>
/// </remarks>
internal sealed class GDPROptionsValidator : IValidateOptions<GDPROptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GDPROptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.EnforcementMode == GDPREnforcementMode.Enforce)
        {
            if (string.IsNullOrWhiteSpace(options.ControllerName))
            {
                failures.Add(
                    "GDPROptions.ControllerName is required when EnforcementMode is Enforce. "
                    + "Article 30(1)(a) requires the controller's identity.");
            }

            if (string.IsNullOrWhiteSpace(options.ControllerEmail))
            {
                failures.Add(
                    "GDPROptions.ControllerEmail is required when EnforcementMode is Enforce. "
                    + "Article 30(1)(a) requires the controller's contact details.");
            }
        }

        if (options.DataProtectionOfficer is not null)
        {
            if (string.IsNullOrWhiteSpace(options.DataProtectionOfficer.Name))
            {
                failures.Add(
                    "DataProtectionOfficer.Name cannot be empty. "
                    + "Article 37(7) requires the DPO's identity to be communicated.");
            }

            if (string.IsNullOrWhiteSpace(options.DataProtectionOfficer.Email))
            {
                failures.Add(
                    "DataProtectionOfficer.Email cannot be empty. "
                    + "Article 37(7) requires the DPO's contact details.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
