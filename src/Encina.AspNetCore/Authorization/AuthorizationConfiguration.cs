namespace Encina.AspNetCore.Authorization;

/// <summary>
/// Configuration options for Encina's CQRS-aware authorization behavior.
/// </summary>
/// <remarks>
/// <para>
/// This configuration extends ASP.NET Core's authorization system with CQRS-aware
/// defaults. It does <b>not</b> replace ASP.NET Core authorization; instead, it
/// configures how the <see cref="AuthorizationPipelineBehavior{TRequest, TResponse}"/>
/// applies default policies to commands and queries that lack explicit authorization attributes.
/// </para>
/// <para>
/// Both <see cref="DefaultCommandPolicy"/> and <see cref="DefaultQueryPolicy"/> default to
/// <c>"RequireAuthenticated"</c> following the <b>secure-by-default</b> principle.
/// Handlers that need anonymous access should use <c>[AllowAnonymous]</c> explicitly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// builder.Services.AddEncinaAuthorization(auth =>
/// {
///     auth.AutoApplyPolicies = true;
///     auth.DefaultCommandPolicy = "RequireAuthenticated";
///     auth.DefaultQueryPolicy  = "AllowAnonymous"; // opt-in for open queries
/// });
/// </code>
/// </example>
public sealed class AuthorizationConfiguration
{
    /// <summary>
    /// The well-known policy name registered automatically by Encina
    /// when it does not already exist.
    /// </summary>
    public const string RequireAuthenticatedPolicyName = "RequireAuthenticated";

    /// <summary>
    /// Default authorization policy applied to commands (<see cref="ICommand{TResponse}"/>)
    /// when <see cref="AutoApplyPolicies"/> is <c>true</c> and the request type has
    /// no explicit <c>[Authorize]</c> or <c>[AllowAnonymous]</c> attributes.
    /// </summary>
    /// <value>Defaults to <c>"RequireAuthenticated"</c>.</value>
    public string DefaultCommandPolicy { get; set; } = RequireAuthenticatedPolicyName;

    /// <summary>
    /// Default authorization policy applied to queries (<see cref="IQuery{TResponse}"/>)
    /// when <see cref="AutoApplyPolicies"/> is <c>true</c> and the request type has
    /// no explicit <c>[Authorize]</c> or <c>[AllowAnonymous]</c> attributes.
    /// </summary>
    /// <value>Defaults to <c>"RequireAuthenticated"</c>.</value>
    public string DefaultQueryPolicy { get; set; } = RequireAuthenticatedPolicyName;

    /// <summary>
    /// When <c>true</c>, the pipeline behavior automatically applies
    /// <see cref="DefaultCommandPolicy"/> or <see cref="DefaultQueryPolicy"/>
    /// to requests that have no explicit authorization attributes.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    /// <remarks>
    /// When disabled, only requests decorated with <c>[Authorize]</c> are subject
    /// to authorization checks. Enable this to enforce authentication by default
    /// across all CQRS handlers.
    /// </remarks>
    public bool AutoApplyPolicies { get; set; }

    /// <summary>
    /// When <c>true</c> and <see cref="AutoApplyPolicies"/> is also <c>true</c>,
    /// unauthenticated requests without explicit <c>[AllowAnonymous]</c> are rejected.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool RequireAuthenticationByDefault { get; set; } = true;
}
