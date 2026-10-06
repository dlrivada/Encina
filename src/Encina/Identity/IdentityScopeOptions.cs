namespace Encina;

/// <summary>
/// Options of a service or principal scope opened through <see cref="IRequestContextScopeFactory"/>.
/// </summary>
/// <param name="TenantId">
/// The tenant of the scope. A scope never inherits the ambient tenant: for
/// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/> this is the only tenant
/// (<see langword="null"/> means no tenant); for
/// <see cref="IRequestContextScopeFactory.RunAsPrincipalAsync{T}"/> it is used only when the
/// principal has no tenant claim, and a different value is refused with
/// <see cref="RequestIdentityErrorCodes.TenantConflict"/>.
/// </param>
/// <param name="AllowOverInbound">
/// Opts in, for this call only, to opening the scope inside an inbound request (or a flow forked
/// from one, ended or not), which is refused by default so an anonymous or untrusted request cannot
/// elevate itself into a service identity. The opening is logged at Warning (EventId 174). It never
/// allows opening over a user identity.
/// </param>
/// <example>
/// <code>
/// var result = await scopes.RunAsServiceAsync(
///     "billing-reconciliation",
///     (context, ct) => encina.Send(new ReconcileInvoices(), ct),
///     new IdentityScopeOptions(TenantId: tenantId),
///     ct);
/// </code>
/// </example>
public sealed record IdentityScopeOptions(string? TenantId = null, bool AllowOverInbound = false);
