namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Persistence entity for <see cref="Model.PhasedReport"/>.
/// </summary>
/// <remarks>
/// <para>
/// This entity provides a database-agnostic representation of a phased report,
/// using primitive types suitable for any storage provider (ADO.NET, Dapper, EF Core, MongoDB).
/// </para>
/// <para>
/// Phased reports are stored in a separate table with a foreign key to the breach records
/// table, following a normalized schema for efficient querying by breach identifier.
/// </para>
/// <para>
/// Use <see cref="PhasedReportMapper"/> to convert between this entity and
/// <see cref="Model.PhasedReport"/>.
/// </para>
/// </remarks>
public sealed class PhasedReportEntity
{
    /// <summary>
    /// Unique identifier for this phased report.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Identifier of the breach this report belongs to.
    /// </summary>
    /// <remarks>
    /// Foreign key to the breach records table.
    /// An INDEX should be created on this column for efficient breach-based lookups.
    /// </remarks>
    public required string BreachId { get; set; }

    /// <summary>
    /// Sequential report number, starting at 1 for the initial report.
    /// </summary>
    public required int ReportNumber { get; set; }

    /// <summary>
    /// Content of the phased report.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Timestamp when this phased report was submitted (UTC).
    /// </summary>
    public DateTimeOffset SubmittedAtUtc { get; set; }

    /// <summary>
    /// Identifier of the user who submitted this phased report.
    /// </summary>
    public string? SubmittedByUserId { get; set; }
}
