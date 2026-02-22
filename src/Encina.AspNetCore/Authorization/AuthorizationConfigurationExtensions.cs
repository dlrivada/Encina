using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Convenience helpers for registering common ASP.NET Core authorization policies.
/// </summary>
/// <remarks>
/// <para>
/// These methods are thin wrappers over <see cref="AuthorizationOptions"/>; they
/// do <b>not</b> create any parallel authorization infrastructure. Every policy
/// registered here is a standard ASP.NET Core policy that can be referenced from
/// <c>[Authorize(Policy = "…")]</c> attributes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddEncinaAuthorization(
///     configurePolicies: policies =>
///     {
///         policies.AddRolePolicy("CanEditOrders", "Admin", "OrderManager");
///         policies.AddClaimPolicy("SalesDepartment", "department", "sales");
///         policies.AddAuthenticatedPolicy("MustBeLoggedIn");
///     });
/// </code>
/// </example>
public static class AuthorizationConfigurationExtensions
{
    /// <summary>
    /// Registers a policy that requires the user to be in at least one of the
    /// specified roles (OR semantics).
    /// </summary>
    /// <param name="options">The ASP.NET Core authorization options.</param>
    /// <param name="policyName">The name of the policy to register.</param>
    /// <param name="roles">One or more roles; the user must hold <b>any</b> of them.</param>
    /// <returns>The <paramref name="options"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/>, <paramref name="policyName"/> or <paramref name="roles"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyName"/> is empty/whitespace or <paramref name="roles"/> is empty.
    /// </exception>
    public static AuthorizationOptions AddRolePolicy(
        this AuthorizationOptions options,
        string policyName,
        params string[] roles)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentNullException.ThrowIfNull(roles);

        if (roles.Length == 0)
        {
            throw new ArgumentException("At least one role must be specified.", nameof(roles));
        }

        options.AddPolicy(policyName, policy => policy
            .RequireAuthenticatedUser()
            .RequireRole(roles));

        return options;
    }

    /// <summary>
    /// Registers a policy that requires the user to hold a specific claim.
    /// </summary>
    /// <param name="options">The ASP.NET Core authorization options.</param>
    /// <param name="policyName">The name of the policy to register.</param>
    /// <param name="claimType">The claim type that must be present.</param>
    /// <param name="allowedValues">
    /// Allowed values for the claim. When empty, only the <b>existence</b> of the
    /// claim is verified.
    /// </param>
    /// <returns>The <paramref name="options"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/>, <paramref name="policyName"/> or <paramref name="claimType"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyName"/> or <paramref name="claimType"/> is empty/whitespace.
    /// </exception>
    public static AuthorizationOptions AddClaimPolicy(
        this AuthorizationOptions options,
        string policyName,
        string claimType,
        params string[] allowedValues)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimType);

        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();

            if (allowedValues.Length > 0)
            {
                policy.RequireClaim(claimType, allowedValues);
            }
            else
            {
                policy.RequireClaim(claimType);
            }
        });

        return options;
    }

    /// <summary>
    /// Registers a policy that only requires the user to be authenticated.
    /// </summary>
    /// <param name="options">The ASP.NET Core authorization options.</param>
    /// <param name="policyName">The name of the policy to register.</param>
    /// <returns>The <paramref name="options"/> instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="policyName"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="policyName"/> is empty/whitespace.
    /// </exception>
    public static AuthorizationOptions AddAuthenticatedPolicy(
        this AuthorizationOptions options,
        string policyName)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        options.AddPolicy(policyName, policy =>
            policy.RequireAuthenticatedUser());

        return options;
    }
}
