using Microsoft.Extensions.Options;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Validates <see cref="DataSubjectRightsOptions"/> at startup to catch configuration errors early.
/// </summary>
/// <remarks>
/// <para>
/// Validates that DSR configuration is consistent and complete.
/// This validator runs during the first <c>IOptions&lt;DataSubjectRightsOptions&gt;.Value</c> access,
/// providing fail-fast behavior for misconfigured DSR compliance.
/// </para>
/// <para>
/// Validates:
/// <list type="bullet">
/// <item><description><see cref="DataSubjectRightsOptions.DefaultDeadlineDays"/> is positive</description></item>
/// <item><description><see cref="DataSubjectRightsOptions.MaxExtensionDays"/> is non-negative and at most 60</description></item>
/// </list>
/// </para>
/// </remarks>
internal sealed class DataSubjectRightsOptionsValidator : IValidateOptions<DataSubjectRightsOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DataSubjectRightsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        // Validate DefaultDeadlineDays
        if (options.DefaultDeadlineDays <= 0)
        {
            failures.Add(
                "DataSubjectRightsOptions.DefaultDeadlineDays must be a positive number. "
                + "GDPR Article 12(3) requires response within one month (approximately 30 days).");
        }

        // Validate MaxExtensionDays
        if (options.MaxExtensionDays < 0)
        {
            failures.Add(
                "DataSubjectRightsOptions.MaxExtensionDays must be non-negative. "
                + "Set to 0 to disallow deadline extensions.");
        }

        if (options.MaxExtensionDays > 60)
        {
            failures.Add(
                "DataSubjectRightsOptions.MaxExtensionDays cannot exceed 60. "
                + "GDPR Article 12(3) limits extensions to a further two months (60 days).");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
