using Microsoft.Extensions.Options;

namespace Encina.Audit.Marten;

/// <summary>
/// Validates <see cref="MartenOperationAuditOptions"/>: a configured
/// <see cref="MartenOperationAuditOptions.RetentionPolicyId"/> must not be empty or whitespace.
/// </summary>
internal sealed class MartenOperationAuditOptionsValidator : IValidateOptions<MartenOperationAuditOptions>
{
    internal const string RetentionPolicyIdBlankMessage =
        "MartenOperationAuditOptions.RetentionPolicyId must not be empty or whitespace; leave it null to derive the policy id from RetentionPeriod.";

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MartenOperationAuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.RetentionPolicyId is { } policyId && string.IsNullOrWhiteSpace(policyId)
            ? ValidateOptionsResult.Fail(RetentionPolicyIdBlankMessage)
            : ValidateOptionsResult.Success;
    }
}
