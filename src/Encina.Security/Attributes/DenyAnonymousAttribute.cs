namespace Encina.Security;

/// <summary>
/// Requires the user to be authenticated. Anonymous requests are denied.
/// </summary>
/// <remarks>
/// <para>
/// This is a simple marker attribute that checks <see cref="ISecurityContext.IsAuthenticated"/>.
/// It is the most basic security gate and is typically evaluated first.
/// </para>
/// <para>
/// Unlike ASP.NET Core's <c>[Authorize]</c>, this attribute operates at the CQRS pipeline level,
/// ensuring consistent enforcement across all transports (HTTP, messaging, gRPC, etc.).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [DenyAnonymous]
/// public sealed record GetProfileQuery() : IQuery&lt;ProfileDto&gt;;
///
/// // Combine with other security checks
/// [DenyAnonymous]
/// [RequirePermission("orders:create")]
/// public sealed record CreateOrderCommand(OrderData Data) : ICommand;
/// </code>
/// </example>
public sealed class DenyAnonymousAttribute : SecurityAttribute;
