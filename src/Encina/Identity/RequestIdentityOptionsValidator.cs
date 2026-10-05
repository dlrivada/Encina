using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Validates <see cref="RequestIdentityOptions"/> at startup: every claim-type list is non-empty and
/// has no blank entries.
/// </summary>
/// <example>
/// <code>
/// var result = new RequestIdentityOptionsValidator().Validate(null, options);
/// if (result.Failed) { /* result.Failures lists each bad list */ }
/// </code>
/// </example>
internal sealed class RequestIdentityOptionsValidator : IValidateOptions<RequestIdentityOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RequestIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        CheckList(options.UserIdClaimTypes, nameof(RequestIdentityOptions.UserIdClaimTypes), failures);
        CheckList(options.RoleClaimTypes, nameof(RequestIdentityOptions.RoleClaimTypes), failures);
        CheckList(options.PermissionClaimTypes, nameof(RequestIdentityOptions.PermissionClaimTypes), failures);
        CheckList(options.TenantIdClaimTypes, nameof(RequestIdentityOptions.TenantIdClaimTypes), failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void CheckList(IList<string> claimTypes, string listName, List<string> failures)
    {
        if (claimTypes.Count == 0)
        {
            failures.Add($"RequestIdentityOptions.{listName} must contain at least one claim type.");
            return;
        }

        if (claimTypes.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add($"RequestIdentityOptions.{listName} must not contain blank claim types.");
        }
    }
}
