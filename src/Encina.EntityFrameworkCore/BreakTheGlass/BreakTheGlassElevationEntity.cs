using Encina.Security.BreakTheGlass;

namespace Encina.EntityFrameworkCore.BreakTheGlass;

/// <summary>
/// The persisted form of a <see cref="BreakTheGlassElevation"/> (table <c>BreakTheGlassElevations</c>).
/// </summary>
/// <remarks>
/// Timestamps are UTC <see cref="DateTime"/> values because the SQL Server columns are <c>datetime2(7)</c>.
/// <see cref="ActiveKey"/> is computed by the database and never written by the application.
/// </remarks>
public sealed class BreakTheGlassElevationEntity
{
    /// <summary>Gets or sets the id of the elevation (primary key).</summary>
    public Guid ElevationId { get; set; }

    /// <summary>Gets or sets the tenant, or <see langword="null"/> when tenancy is off.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the id of the user who opened the elevation.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the data subject whose data the elevation opens.</summary>
    public string SubjectId { get; set; } = string.Empty;

    /// <summary>Gets or sets the named scope.</summary>
    public string Scope { get; set; } = string.Empty;

    /// <summary>Gets or sets the justification.</summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>Gets or sets the instant (UTC) the elevation was opened.</summary>
    public DateTime OpenedAtUtc { get; set; }

    /// <summary>Gets or sets the instant (UTC) the elevation stops working.</summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>Gets or sets the lifecycle status.</summary>
    public BreakTheGlassStatus Status { get; set; }

    /// <summary>Gets or sets the instant (UTC) the elevation was closed.</summary>
    public DateTime? ClosedAtUtc { get; set; }

    /// <summary>Gets or sets the fixed reason code of the close.</summary>
    public string? CloseReason { get; set; }

    /// <summary>Gets or sets the instant (UTC) the expiry was recorded in the audit trail.</summary>
    public DateTime? ExpiryRecordedAtUtc { get; set; }

    /// <summary>Gets or sets the review outcome.</summary>
    public BreakTheGlassReviewOutcome? ReviewOutcome { get; set; }

    /// <summary>Gets or sets the id of the reviewer.</summary>
    public string? ReviewedByUserId { get; set; }

    /// <summary>Gets or sets the instant (UTC) of the review.</summary>
    public DateTime? ReviewedAtUtc { get; set; }

    /// <summary>Gets or sets the reviewer's note.</summary>
    public string? ReviewNote { get; set; }

    /// <summary>
    /// Gets the database-computed arbiter of "one active elevation per tenant, user, subject and scope": a hash of the key
    /// parts while the row is Active, and the elevation id otherwise.
    /// </summary>
    public byte[]? ActiveKey { get; private set; }
}
