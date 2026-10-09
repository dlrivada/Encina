using System.Text;

using Encina.Security.Audit;

namespace Encina.Security.ABAC.DecisionAudit;

/// <summary>
/// The filters and the page of a decision audit query. Every filter is optional and runs on the
/// store side; filters combine with AND.
/// </summary>
/// <remarks>
/// <para>
/// A filter value longer than its column limit is hashed exactly as the stored value was, so a
/// subject, request type, resource or tenant stored as <c>sha256:&lt;64 hex&gt;</c> is still found.
/// </para>
/// <para>
/// The tenant filter is subject to the reader's tenant gate: the request's own tenant is always
/// forced, and a different tenant is denied (see <see cref="IABACDecisionAuditReader"/>).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var page = await reader.QueryAsync(new ABACDecisionAuditQuery
/// {
///     UserId = "user-42",
///     Outcome = AuditOutcome.Denied,
///     FromUtc = DateTime.UtcNow.AddDays(-7),
///     PageSize = 100
/// }, cancellationToken);
/// </code>
/// </example>
public sealed record ABACDecisionAuditQuery
{
    /// <summary>The caller's user id (or <c>service:&lt;name&gt;</c>).</summary>
    public string? UserId { get; init; }

    /// <summary>The tenant; when the request carries a tenant it must be that tenant.</summary>
    public string? TenantId { get; init; }

    /// <summary>The request type name (the XACML action).</summary>
    public string? RequestType { get; init; }

    /// <summary>The declared resource id.</summary>
    public string? ResourceId { get; init; }

    /// <summary>The stored audit outcome.</summary>
    public AuditOutcome? Outcome { get; init; }

    /// <summary>The correlation id of the request.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>The start of the time range (inclusive), in UTC.</summary>
    public DateTime? FromUtc { get; init; }

    /// <summary>The end of the time range (inclusive), in UTC; it must not precede <see cref="FromUtc"/>.</summary>
    public DateTime? ToUtc { get; init; }

    /// <summary>The page number, starting at 1. Ignored by the export, which reads every page.</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// The page size, from 1 to <see cref="OperationAuditQuery.MaxPageSize"/>. Default is
    /// <see cref="OperationAuditQuery.DefaultPageSize"/>. Ignored by the export, which reads pages of
    /// <see cref="OperationAuditQuery.MaxPageSize"/>.
    /// </summary>
    public int PageSize { get; init; } = OperationAuditQuery.DefaultPageSize;

    /// <summary>Prints only the paging, so a log never receives the user, tenant or resource filters.</summary>
    private bool PrintMembers(StringBuilder builder)
    {
        builder.Append("PageNumber = ").Append(PageNumber)
            .Append(", PageSize = ").Append(PageSize);
        return true;
    }
}
