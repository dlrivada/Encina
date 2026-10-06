using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace Encina;

/// <summary>
/// Validates the declared service identities at startup (<c>ValidateOnStart</c>).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>Names match <c>^[a-z0-9][a-z0-9.-]{0,62}$</c>.</description></item>
/// <item><description>The <c>encina.</c> prefix is reserved for built-in identities of Encina
/// packages, and every built-in identity uses it.</description></item>
/// <item><description>A name is declared once (an identical repeat is not a conflict).</description></item>
/// <item><description>No role or permission contains the wildcard <c>*</c>.</description></item>
/// <item><description>No declared claim uses a user-id claim type of
/// <see cref="RequestIdentityOptions.UserIdClaimTypes"/> or the identity-kind claim type: the
/// subject of a service is always <c>service:&lt;name&gt;</c>.</description></item>
/// </list>
/// </remarks>
internal sealed partial class ServiceIdentityCatalogOptionsValidator : IValidateOptions<ServiceIdentityCatalogOptions>
{
    /// <summary>
    /// The name prefix reserved for built-in identities of Encina packages.
    /// </summary>
    internal const string BuiltInPrefix = "encina.";

    private readonly RequestIdentityOptions _identityOptions;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceIdentityCatalogOptionsValidator"/> class.
    /// </summary>
    /// <param name="identityOptions">The claim map, for the user-id claim types a declaration must not use.</param>
    public ServiceIdentityCatalogOptionsValidator(IOptions<RequestIdentityOptions> identityOptions)
    {
        ArgumentNullException.ThrowIfNull(identityOptions);
        _identityOptions = identityOptions.Value;
    }

    /// <summary>
    /// Determines whether <paramref name="name"/> matches the service-identity name pattern.
    /// </summary>
    internal static bool IsValidName(string? name) => name is not null && NamePattern().IsMatch(name);

    /// <summary>
    /// Determines whether <paramref name="name"/> uses the reserved built-in prefix.
    /// </summary>
    internal static bool IsReservedName(string name) => name.StartsWith(BuiltInPrefix, StringComparison.Ordinal);

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, ServiceIdentityCatalogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = options.ConflictingNames
            .Select(static conflict => $"Service identity '{conflict}' is declared more than once with different roles, permissions or claims.")
            .ToList();

        foreach (var definition in options.Identities.Values)
        {
            failures.AddRange(Check(definition));
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private IEnumerable<string> Check(ServiceIdentityDefinition definition)
    {
        if (!IsValidName(definition.Name))
        {
            yield return $"Service identity '{definition.Name}' does not match the name pattern ^[a-z0-9][a-z0-9.-]{{0,62}}$.";
        }

        if (IsReservedName(definition.Name) != definition.IsBuiltIn)
        {
            yield return $"Service identity '{definition.Name}': the '{BuiltInPrefix}' prefix is reserved for built-in identities of Encina packages.";
        }

        if (definition.Roles.Concat(definition.Permissions).Any(static entry => entry.Contains('*', StringComparison.Ordinal)))
        {
            yield return $"Service identity '{definition.Name}' declares a wildcard role or permission; declare each one explicitly.";
        }

        if (definition.Claims.Any(claim => IsForbiddenClaimType(claim.Key)))
        {
            yield return $"Service identity '{definition.Name}' declares a claim with a user-id or identity-kind claim type; the subject of a service is always 'service:<name>'.";
        }
    }

    private bool IsForbiddenClaimType(string claimType) =>
        string.Equals(claimType, RequestIdentity.IdentityKindClaimType, StringComparison.OrdinalIgnoreCase)
        || _identityOptions.UserIdClaimTypes.Contains(claimType, StringComparer.OrdinalIgnoreCase);

    [GeneratedRegex("^[a-z0-9][a-z0-9.-]{0,62}$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex NamePattern();
}
