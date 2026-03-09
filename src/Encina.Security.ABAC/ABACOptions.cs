using System.Reflection;

namespace Encina.Security.ABAC;

/// <summary>
/// Configuration options for the ABAC pipeline behavior and XACML 3.0 engine.
/// </summary>
/// <remarks>
/// <para>
/// These options control how <see cref="ABACPipelineBehavior{TRequest, TResponse}"/>
/// evaluates authorization decisions and enforces XACML 3.0 policies.
/// </para>
/// <para>
/// Register via <c>AddEncinaABAC(options => { ... })</c> to configure the pipeline behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddEncinaABAC(options =>
/// {
///     options.EnforcementMode = ABACEnforcementMode.Block;
///     options.DefaultNotApplicableEffect = Effect.Deny;
///     options.IncludeAdvice = true;
///     options.FailOnMissingObligationHandler = true;
///     options.AddHealthCheck = true;
///
///     // Register custom functions
///     options.AddFunction("custom:geo-distance", new GeoDistanceFunction());
///
///     // Seed policies at startup
///     options.SeedPolicySets.Add(myPolicySet);
///     options.SeedPolicies.Add(myStandalonePolicy);
/// });
/// </code>
/// </example>
public sealed class ABACOptions
{
    /// <summary>
    /// Gets or sets the enforcement mode for ABAC authorization decisions.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description><see cref="ABACEnforcementMode.Block"/> — Deny decisions block request execution.</description></item>
    /// <item><description><see cref="ABACEnforcementMode.Warn"/> — Deny decisions are logged but requests proceed.</description></item>
    /// <item><description><see cref="ABACEnforcementMode.Disabled"/> — ABAC evaluation is completely skipped.</description></item>
    /// </list>
    /// Default is <see cref="ABACEnforcementMode.Block"/>.
    /// </remarks>
    public ABACEnforcementMode EnforcementMode { get; set; } = ABACEnforcementMode.Block;

    /// <summary>
    /// Gets or sets the effect to apply when no policy matches the request (NotApplicable).
    /// </summary>
    /// <remarks>
    /// <para>
    /// XACML 3.0 leaves the handling of NotApplicable to the PEP. Common choices:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="Effect.Deny"/> — closed-world assumption; unmatched requests are denied (default).</description></item>
    /// <item><description><see cref="Effect.Permit"/> — open-world assumption; unmatched requests are allowed.</description></item>
    /// </list>
    /// Default is <see cref="Effect.Deny"/> (secure by default).
    /// </remarks>
    public Effect DefaultNotApplicableEffect { get; set; } = Effect.Deny;

    /// <summary>
    /// Gets or sets whether to include advice expressions in policy evaluation results.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the <see cref="PolicyEvaluationContext.IncludeAdvice"/> flag is set,
    /// and any advice returned by the PDP is executed on a best-effort basis.
    /// Default is <c>true</c>.
    /// </remarks>
    public bool IncludeAdvice { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to fail with an error when an obligation has no registered handler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XACML 3.0 section 7.18 mandates that if an obligation cannot be fulfilled, the PEP must
    /// deny access. When <c>true</c> (default), a missing handler causes an immediate deny.
    /// </para>
    /// <para>
    /// Set to <c>false</c> during development to allow soft-fail when handlers are not yet
    /// implemented. <b>Must be <c>true</c> in production.</b>
    /// </para>
    /// </remarks>
    public bool FailOnMissingObligationHandler { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to register an ABAC health check.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, an <see cref="Health.ABACHealthCheck"/> is registered that verifies
    /// at least one policy or policy set is loaded. Returns <c>Degraded</c> if the PAP is empty,
    /// <c>Healthy</c> if at least one policy exists.
    /// Default is <c>false</c>.
    /// </remarks>
    public bool AddHealthCheck { get; set; }

    /// <summary>
    /// Gets or sets whether to validate all EEL expressions at application startup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, the <see cref="EEL.EELExpressionPrecompilationService"/> scans
    /// assemblies listed in <see cref="ExpressionScanAssemblies"/> for
    /// <see cref="RequireConditionAttribute"/> decorations, compiles every expression,
    /// and throws <see cref="InvalidOperationException"/> if any expression fails to compile.
    /// </para>
    /// <para>
    /// This provides fail-fast behavior: invalid EEL expressions are caught at startup
    /// rather than at request time. Default is <c>false</c>.
    /// </para>
    /// </remarks>
    public bool ValidateExpressionsAtStartup { get; set; }

    /// <summary>
    /// Gets the list of assemblies to scan for <see cref="RequireConditionAttribute"/>
    /// decorations during startup expression validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only used when <see cref="ValidateExpressionsAtStartup"/> is <c>true</c>.
    /// Add assemblies containing your MediatR request types decorated with
    /// <see cref="RequireConditionAttribute"/>.
    /// </para>
    /// <para>
    /// If <see cref="ValidateExpressionsAtStartup"/> is <c>true</c> but this list is empty,
    /// a debug log is emitted and no validation occurs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// options.ValidateExpressionsAtStartup = true;
    /// options.ExpressionScanAssemblies.Add(typeof(MyCommand).Assembly);
    /// </code>
    /// </example>
    public List<Assembly> ExpressionScanAssemblies { get; } = [];

    /// <summary>
    /// Gets or sets whether to use a persistent (database-backed)
    /// <see cref="IPolicyAdministrationPoint"/> instead of the default in-memory implementation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <c>true</c>, <see cref="Administration.PersistentPolicyAdministrationPoint"/> is
    /// registered as the <see cref="IPolicyAdministrationPoint"/> implementation, delegating
    /// policy storage to an <see cref="Persistence.IPolicyStore"/> provided by a database
    /// provider package (EF Core, Dapper, ADO.NET, or MongoDB).
    /// </para>
    /// <para>
    /// An <see cref="Persistence.IPolicySerializer"/> is also registered (default:
    /// <see cref="Persistence.DefaultPolicySerializer"/>) using <c>TryAdd</c>, allowing
    /// custom serializer implementations to be registered before calling
    /// <c>AddEncinaABAC</c>.
    /// </para>
    /// <para>
    /// <b>Prerequisite</b>: A provider package must register an <see cref="Persistence.IPolicyStore"/>
    /// implementation (e.g., <c>services.AddEncinaEntityFrameworkCore(c => c.UseABACPolicyStore = true)</c>).
    /// An <see cref="InvalidOperationException"/> is thrown at resolution time if no store is registered.
    /// </para>
    /// <para>
    /// Default is <c>false</c> (uses <see cref="Administration.InMemoryPolicyAdministrationPoint"/>).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaABAC(options =>
    /// {
    ///     options.UsePersistentPAP = true;
    /// });
    /// </code>
    /// </example>
    public bool UsePersistentPAP { get; set; }

    /// <summary>
    /// Gets the policy caching configuration for the persistent PAP.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only applicable when <see cref="UsePersistentPAP"/> is <c>true</c>. Configures
    /// the <c>CachingPolicyStoreDecorator</c> that wraps the underlying
    /// <see cref="Persistence.IPolicyStore"/> with cache-aside reads and
    /// write-through invalidation.
    /// </para>
    /// <para>
    /// Set <see cref="PolicyCachingOptions.Enabled"/> to <c>true</c> to activate caching.
    /// Requires an <c>ICacheProvider</c> from an Encina caching package.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddEncinaABAC(options =>
    /// {
    ///     options.UsePersistentPAP = true;
    ///     options.PolicyCaching.Enabled = true;
    ///     options.PolicyCaching.Duration = TimeSpan.FromMinutes(15);
    ///     options.PolicyCaching.EnablePubSubInvalidation = true;
    /// });
    /// </code>
    /// </example>
    public PolicyCachingOptions PolicyCaching { get; } = new();

    /// <summary>
    /// Gets the list of custom functions to register in the <see cref="IFunctionRegistry"/>
    /// at startup, in addition to the standard XACML 3.0 functions.
    /// </summary>
    /// <remarks>
    /// Functions added here are registered into the <see cref="IFunctionRegistry"/> singleton
    /// during DI configuration, making them available for policy condition evaluation.
    /// Use unique function identifiers to avoid overwriting standard functions.
    /// </remarks>
    public List<(string FunctionId, IXACMLFunction Function)> CustomFunctions { get; } = [];

    /// <summary>
    /// Gets the list of policy sets to seed into the PAP at application startup.
    /// </summary>
    /// <remarks>
    /// Policy sets in this list are added to the <see cref="IPolicyAdministrationPoint"/>
    /// by the <see cref="ABACPolicySeedingHostedService"/> during application startup.
    /// Duplicate IDs are logged as warnings and skipped.
    /// </remarks>
    public List<PolicySet> SeedPolicySets { get; } = [];

    /// <summary>
    /// Gets the list of standalone policies to seed into the PAP at application startup.
    /// </summary>
    /// <remarks>
    /// Standalone policies (not belonging to any policy set) in this list are added to the
    /// <see cref="IPolicyAdministrationPoint"/> by the <see cref="ABACPolicySeedingHostedService"/>
    /// during application startup. Duplicate IDs are logged as warnings and skipped.
    /// </remarks>
    public List<Policy> SeedPolicies { get; } = [];

    /// <summary>
    /// Registers a custom XACML function for use in policy conditions.
    /// </summary>
    /// <param name="functionId">The unique function identifier.</param>
    /// <param name="function">The function implementation.</param>
    /// <returns>This options instance for chaining.</returns>
    /// <example>
    /// <code>
    /// options.AddFunction("custom:geo-within", new GeoWithinFunction())
    ///        .AddFunction("custom:risk-score", new RiskScoreFunction());
    /// </code>
    /// </example>
    public ABACOptions AddFunction(string functionId, IXACMLFunction function)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionId);
        ArgumentNullException.ThrowIfNull(function);

        CustomFunctions.Add((functionId, function));
        return this;
    }
}
