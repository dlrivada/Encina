using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Validates <see cref="RequestIdentityOptions"/> at startup: every claim-type list is non-empty and
/// has no blank entries, and <see cref="RequestIdentityOptions.PerTokenClaimTypes"/> never excludes
/// a claim that carries authority from identity comparison.
/// </summary>
/// <example>
/// <code>
/// var result = new RequestIdentityOptionsValidator().Validate(null, options);
/// if (result.Failed) { /* result.Failures lists each bad list */ }
/// </code>
/// </example>
internal sealed class RequestIdentityOptionsValidator : IValidateOptions<RequestIdentityOptions>
{
    // Step-up and authentication-strength signals: a change in either is a different identity.
    private static readonly string[] AuthorityClaimTypes = ["amr", "acr"];

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RequestIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        CheckList(options.UserIdClaimTypes, nameof(RequestIdentityOptions.UserIdClaimTypes), failures);
        CheckList(options.RoleClaimTypes, nameof(RequestIdentityOptions.RoleClaimTypes), failures);
        CheckList(options.PermissionClaimTypes, nameof(RequestIdentityOptions.PermissionClaimTypes), failures);
        CheckList(options.TenantIdClaimTypes, nameof(RequestIdentityOptions.TenantIdClaimTypes), failures);
        CheckPerTokenClaimTypes(options, failures);

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

    // The list may be empty (nothing is ignored); it must never exclude a subject, role,
    // permission, amr or acr claim type, which would let an identity change go unnoticed.
    private static void CheckPerTokenClaimTypes(RequestIdentityOptions options, List<string> failures)
    {
        if (options.PerTokenClaimTypes.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("RequestIdentityOptions.PerTokenClaimTypes must not contain blank claim types.");
        }

        var protectedTypes = options.UserIdClaimTypes
            .Concat(options.RoleClaimTypes)
            .Concat(options.PermissionClaimTypes)
            .Concat(AuthorityClaimTypes)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var excluded = options.PerTokenClaimTypes
            .Where(claimType => claimType is not null && protectedTypes.Contains(claimType))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (excluded.Count > 0)
        {
            failures.Add(
                $"RequestIdentityOptions.PerTokenClaimTypes must not name a user-id, role, permission, amr or acr claim type: {string.Join(", ", excluded)}.");
        }
    }
}
