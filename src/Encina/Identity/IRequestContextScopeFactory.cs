using System.Security.Claims;
using LanguageExt;

namespace Encina;

/// <summary>
/// Binds a caller identity to one unit of work: the only way to run code as a declared service, as
/// a principal, as an inbound request or as the restored originator of a deferred message.
/// </summary>
/// <remarks>
/// <para>
/// <b>Delegate-only.</b> A scope is the region in which <c>work</c> runs. Inside it the identity is
/// ambient (<see cref="IRequestContextAccessor"/>) for <c>work</c> and every task it starts; when
/// <c>work</c> completes, faults or is cancelled the scope ends and the identity stops reading as a
/// caller everywhere, including in tasks that outlive it, which then read as anonymous and are
/// denied by every gate. The caller's own context comes back through the <c>async</c> frame; there
/// is no handle to dispose. Run one scope per unit of work (per loop iteration, per message), and
/// consume streams inside <c>work</c>.
/// </para>
/// <para>
/// <b>Refusals</b> are <see cref="EncinaError"/> results, never exceptions, and never invoke
/// <c>work</c>. They are checked in this order:
/// </para>
/// <list type="number">
/// <item><description>The registered accessor is not the default <see cref="RequestContextAccessor"/>:
/// <see cref="RequestIdentityErrorCodes.UnsupportedAccessor"/> (startup also fails).</description></item>
/// <item><description>The token is already cancelled: <see cref="EncinaErrorCodes.RequestCancelled"/>.</description></item>
/// <item><description>The arguments: an undeclared or built-in service name, a tenant that conflicts
/// with the principal, a persisted identity that fails validation.</description></item>
/// <item><description>The ambient chain: a user identity anywhere in the current flow's chain
/// (ended scopes included) refuses every member with <see cref="RequestIdentityErrorCodes.ScopeConflict"/>;
/// an inbound request anywhere in it refuses every member unless
/// <see cref="IdentityScopeOptions.AllowOverInbound"/> is set on a service or principal scope.</description></item>
/// </list>
/// <para>
/// A <c>Left</c> returned by <c>work</c> passes through unchanged, and an exception thrown by
/// <c>work</c> (<see cref="OperationCanceledException"/> included) propagates unchanged after the
/// scope has ended. Every opening, refusal and ending is logged with identity kinds, error codes,
/// correlation ids and declared service names only (EventIds 166-175).
/// </para>
/// <para>
/// The scope API guards against accidental misuse; it is not a security boundary against hostile
/// in-process code (code that suppresses the execution-context flow starts with no chain).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Startup
/// services.AddEncinaServiceIdentity("billing-reconciliation", id => id
///     .WithRoles("billing-job")
///     .WithPermissions("invoices:reconcile"));
///
/// // Job: one scope per unit of work
/// var result = await scopes.RunAsServiceAsync(
///     "billing-reconciliation",
///     (context, ct) => encina.Send(new ReconcileInvoices(), ct),
///     new IdentityScopeOptions(TenantId: tenantId),
///     ct);
/// result.IfLeft(error => logger.LogWarning("Reconciliation failed: {Code}", error.GetCode().IfNone("unknown")));
/// </code>
/// </example>
public interface IRequestContextScopeFactory
{
    /// <summary>
    /// Runs <paramref name="work"/> as the declared service identity <paramref name="serviceId"/>.
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="work"/>.</typeparam>
    /// <param name="serviceId">The name declared with <c>AddEncinaServiceIdentity</c>.</param>
    /// <param name="work">The unit of work; it receives the scope's context and the cancellation token.</param>
    /// <param name="options">The tenant of the scope and the inbound opt-in; none by default.</param>
    /// <param name="cancellationToken">The cancellation token passed to <paramref name="work"/>.</param>
    /// <returns>
    /// The result of <paramref name="work"/>, or a refusal: <see cref="RequestIdentityErrorCodes.UnknownServiceIdentity"/>,
    /// <see cref="RequestIdentityErrorCodes.ReservedServiceIdentity"/>, <see cref="RequestIdentityErrorCodes.ScopeConflict"/>,
    /// <see cref="RequestIdentityErrorCodes.UnsupportedAccessor"/> or <see cref="EncinaErrorCodes.RequestCancelled"/>.
    /// </returns>
    /// <remarks>
    /// The identity carries exactly the declared roles, permissions and claims under the subject
    /// <c>service:&lt;name&gt;</c>, and the tenant of <see cref="IdentityScopeOptions.TenantId"/>
    /// (never the ambient tenant). Opening over another service identity is allowed and logged at
    /// Warning; opening inside a user's request is always refused.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="serviceId"/> or <paramref name="work"/> is <see langword="null"/>.</exception>
    Task<Either<EncinaError, T>> RunAsServiceAsync<T>(
        string serviceId,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> as the identity mapped from <paramref name="principal"/> with
    /// <see cref="RequestIdentityOptions"/> (the same mapping as inbound requests).
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="work"/>.</typeparam>
    /// <param name="principal">The principal to run as; an unauthenticated one opens an anonymous scope.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="options">
    /// The tenant (used only when the principal has no tenant claim; a different value is refused with
    /// <see cref="RequestIdentityErrorCodes.TenantConflict"/>) and the inbound opt-in.
    /// </param>
    /// <param name="cancellationToken">The cancellation token passed to <paramref name="work"/>.</param>
    /// <returns>The result of <paramref name="work"/>, or a refusal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> or <paramref name="work"/> is <see langword="null"/>.</exception>
    Task<Either<EncinaError, T>> RunAsPrincipalAsync<T>(
        ClaimsPrincipal principal,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        IdentityScopeOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> as one inbound unit of work described by
    /// <paramref name="request"/> (for example one server-sent event or one hub invocation).
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="work"/>.</typeparam>
    /// <param name="request">What the entry point read from its caller.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="cancellationToken">The cancellation token passed to <paramref name="work"/>.</param>
    /// <returns>
    /// The result of <paramref name="work"/>, or one of the host conditions
    /// <see cref="RequestIdentityErrorCodes.UnsupportedAccessor"/>, <see cref="EncinaErrorCodes.RequestCancelled"/>
    /// and <see cref="RequestIdentityErrorCodes.ScopeConflict"/>. Client-controlled input is
    /// normalized, never refused (see <see cref="InboundRequestInfo"/>).
    /// </returns>
    /// <remarks>
    /// The tenant is the principal's tenant claim, otherwise the normalized tenant header value.
    /// The context is marked as inbound, so service and principal scopes opened inside it need
    /// <see cref="IdentityScopeOptions.AllowOverInbound"/>. Each call is logged at Information
    /// (EventId 175) as an application binding path.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> or <paramref name="work"/> is <see langword="null"/>.</exception>
    Task<Either<EncinaError, T>> RunInboundAsync<T>(
        InboundRequestInfo request,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs <paramref name="work"/> as the originating actor of a deferred message (SPEC-002 REQ-015),
    /// rebuilt from <paramref name="persisted"/>. One scope per message.
    /// </summary>
    /// <typeparam name="T">The result type of <paramref name="work"/>.</typeparam>
    /// <param name="persisted">The identity stored with the message.</param>
    /// <param name="source">Where the dispatcher read the message from; required.</param>
    /// <param name="work">The unit of work.</param>
    /// <param name="configuredTenantId">
    /// For <see cref="PersistedIdentitySource.External"/> only: the tenant from the dispatcher's
    /// trusted configuration (a per-inbox or per-endpoint mapping, never a value read from the
    /// message); <see langword="null"/> runs the message with no tenant. Ignored for
    /// <see cref="PersistedIdentitySource.Internal"/>, which uses the persisted tenant.
    /// </param>
    /// <param name="cancellationToken">The cancellation token passed to <paramref name="work"/>.</param>
    /// <returns>
    /// The result of <paramref name="work"/>, or a refusal; every validation failure is
    /// <see cref="RequestIdentityErrorCodes.InvalidPersistedIdentity"/> and the dispatcher fails the
    /// message by its retry or dead-letter rules. Always validated: the <paramref name="source"/>,
    /// the correlation and causation ids, and (for <see cref="PersistedIdentitySource.External"/>)
    /// <paramref name="configuredTenantId"/>. For <see cref="PersistedIdentitySource.Internal"/> also
    /// the kind, the actor and the persisted tenant; an external row's kind, actor and tenant are
    /// ignored, never validated, because it always runs anonymous.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A row is never a source of authority: a restored user carries no roles and no permissions
    /// (role- or permission-gated handlers deny), and a restored service takes its authority from
    /// the declaration in the catalog. Built-in identities are never restored.
    /// </para>
    /// <para>
    /// An <see cref="PersistedIdentitySource.External"/> message runs as anonymous whatever the row
    /// says, marked as inbound (service and principal scopes inside it need
    /// <see cref="IdentityScopeOptions.AllowOverInbound"/>), with only <paramref name="configuredTenantId"/>
    /// as its tenant.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="persisted"/> or <paramref name="work"/> is <see langword="null"/>.</exception>
    Task<Either<EncinaError, T>> RunRestoredAsync<T>(
        PersistedRequestIdentity persisted,
        PersistedIdentitySource source,
        Func<IRequestContext, CancellationToken, Task<Either<EncinaError, T>>> work,
        string? configuredTenantId = null,
        CancellationToken cancellationToken = default);
}
