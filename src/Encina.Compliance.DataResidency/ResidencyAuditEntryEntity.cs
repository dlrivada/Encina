namespace Encina.Compliance.DataResidency;

/// <summary>
/// Persistence entity for <see cref="Model.ResidencyAuditEntry"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a residency audit entry,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Key type transformations:
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="Model.ResidencyAuditEntry.Action"/> (<see cref="Model.ResidencyAction"/>) is stored
/// as <see cref="ActionValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="Model.ResidencyAuditEntry.Outcome"/> (<see cref="Model.ResidencyOutcome"/>) is stored
/// as <see cref="OutcomeValue"/> (<see cref="int"/>) for cross-provider compatibility.
/// </description>
/// </item>
/// </list>
/// </para>
/// <para>
/// Use <see cref="ResidencyAuditEntryMapper"/> to convert between this entity and
/// <see cref="Model.ResidencyAuditEntry"/>.
/// </para>
/// </remarks>
public sealed class ResidencyAuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the data entity affected by this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> for actions that are not entity-specific.
    /// </remarks>
    public string? EntityId { get; set; }

    /// <summary>
    /// The data category affected by this action.
    /// </summary>
    public required string DataCategory { get; set; }

    /// <summary>
    /// The source region code for this action.
    /// </summary>
    public required string SourceRegion { get; set; }

    /// <summary>
    /// The target region code for this action.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when no cross-border transfer is involved.
    /// </remarks>
    public string? TargetRegion { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.ResidencyAction"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: PolicyCheck=0, CrossBorderTransfer=1, LocationRecord=2, Violation=3, RegionRouting=4.
    /// </remarks>
    public required int ActionValue { get; set; }

    /// <summary>
    /// Integer value of the <see cref="Model.ResidencyOutcome"/> enum.
    /// </summary>
    /// <remarks>
    /// Values: Allowed=0, Blocked=1, Warning=2, Skipped=3.
    /// </remarks>
    public required int OutcomeValue { get; set; }

    /// <summary>
    /// The legal basis applied for the action, if applicable.
    /// </summary>
    public string? LegalBasis { get; set; }

    /// <summary>
    /// The type of the request that triggered this action.
    /// </summary>
    public string? RequestType { get; set; }

    /// <summary>
    /// Identifier of the user or system that triggered this action.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the action occurred (UTC).
    /// </summary>
    public DateTimeOffset TimestampUtc { get; set; }

    /// <summary>
    /// Additional details about the action.
    /// </summary>
    public string? Details { get; set; }
}
