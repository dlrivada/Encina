using Encina.Security.Audit;

using LanguageExt;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// Reads the ABAC decision audit trail: a typed query by subject, request type, resource, outcome,
/// tenant, correlation id and time range, and a JSON Lines export for compliance.
/// </summary>
/// <remarks>
/// <para>
/// <b>Authorize access to the reader.</b> It exposes the access history of every subject it can
/// see and does not authorize its callers itself: put it behind the application's own
/// authorization (it is a plain service, not an endpoint).
/// </para>
/// <para>
/// <b>Tenant gate</b> (fails closed): when the request carries a tenant, that tenant is always
/// forced and a query naming another tenant is denied with
/// <see cref="ABACErrors.DecisionAuditTenantMismatchCode"/>. When the request carries none and
/// multi-tenancy is enabled (<see cref="MultiTenancyMarker"/> is registered), the query is denied
/// with <see cref="ABACErrors.DecisionAuditTenantRequiredCode"/> unless
/// <see cref="ABACDecisionAuditOptions.AllowCrossTenantQueries"/> is set (a logged opt-out for
/// operator tooling). A single-tenant application (no marker) queries without a tenant.
/// </para>
/// <para>
/// <b>Errors</b>: an invalid page or time range gives <see cref="ABACErrors.InvalidDecisionAuditQueryCode"/>;
/// no registered <see cref="IOperationAuditStore"/> gives <see cref="ABACErrors.DecisionAuditStoreUnavailableCode"/>;
/// a store failure is returned as the store reported it.
/// </para>
/// </remarks>
public interface IABACDecisionAuditReader
{
    /// <summary>
    /// Returns one page of the stored decisions matching <paramref name="query"/>, newest first.
    /// </summary>
    /// <param name="query">The filters and the page.</param>
    /// <param name="cancellationToken">A token to cancel the read.</param>
    /// <returns>The page of decisions, or <c>Left</c> with the denial or failure.</returns>
    ValueTask<Either<EncinaError, PagedResult<ABACDecisionAuditRecord>>> QueryAsync(
        ABACDecisionAuditQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes every stored decision matching the filters of <paramref name="query"/> to
    /// <paramref name="destination"/> as JSON Lines (UTF-8, one <see cref="ABACDecisionAuditRecord"/>
    /// per line, schema <see cref="ABACDecisionAuditSchema.SchemaVersion"/>), reading pages of
    /// <see cref="OperationAuditQuery.MaxPageSize"/>. The paging of <paramref name="query"/> is ignored.
    /// </summary>
    /// <param name="query">The filters.</param>
    /// <param name="destination">The writable stream that receives the lines; it is not closed.</param>
    /// <param name="cancellationToken">A token to cancel the export.</param>
    /// <returns>The number of decisions written, or <c>Left</c> with the denial or failure (lines already written stay written).</returns>
    ValueTask<Either<EncinaError, int>> ExportAsync(
        ABACDecisionAuditQuery query,
        Stream destination,
        CancellationToken cancellationToken = default);
}
