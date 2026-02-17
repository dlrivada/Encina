namespace Encina.Security;

/// <summary>
/// Bypasses all security checks on the decorated request type.
/// </summary>
/// <remarks>
/// <para>
/// When present, <c>SecurityPipelineBehavior</c> skips all security attribute evaluation
/// and proceeds directly to the next pipeline step. This is the Encina equivalent of
/// ASP.NET Core's <c>[AllowAnonymous]</c>, operating at the CQRS pipeline level.
/// </para>
/// <para>
/// Use this for public endpoints that should not require any authentication or authorization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Public health check - no security required
/// [AllowAnonymous]
/// public sealed record HealthCheckQuery() : IQuery&lt;HealthStatus&gt;;
///
/// // Override class-level security for a specific request
/// [AllowAnonymous]
/// public sealed record GetPublicCatalogQuery() : IQuery&lt;CatalogDto&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class AllowAnonymousAttribute : Attribute;
