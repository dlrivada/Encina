using Encina.Compliance.BreachNotification.Model;

namespace Encina.Compliance.BreachNotification;

/// <summary>
/// Maps between <see cref="PhasedReport"/> domain records and
/// <see cref="PhasedReportEntity"/> persistence entities.
/// </summary>
/// <remarks>
/// <para>
/// This is a straightforward property-to-property mapper with no type transformations.
/// All properties are primitive types compatible across all storage providers.
/// </para>
/// <para>
/// Used by store implementations (ADO.NET, Dapper, EF Core, MongoDB) to persist and
/// retrieve phased reports without coupling to the domain model.
/// </para>
/// </remarks>
public static class PhasedReportMapper
{
    /// <summary>
    /// Converts a domain <see cref="PhasedReport"/> to a persistence entity.
    /// </summary>
    /// <param name="report">The domain report to convert.</param>
    /// <returns>A <see cref="PhasedReportEntity"/> suitable for persistence.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is <c>null</c>.</exception>
    public static PhasedReportEntity ToEntity(PhasedReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new PhasedReportEntity
        {
            Id = report.Id,
            BreachId = report.BreachId,
            ReportNumber = report.ReportNumber,
            Content = report.Content,
            SubmittedAtUtc = report.SubmittedAtUtc,
            SubmittedByUserId = report.SubmittedByUserId
        };
    }

    /// <summary>
    /// Converts a persistence entity back to a domain <see cref="PhasedReport"/>.
    /// </summary>
    /// <param name="entity">The persistence entity to convert.</param>
    /// <returns>A <see cref="PhasedReport"/> converted from the entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entity"/> is <c>null</c>.</exception>
    public static PhasedReport ToDomain(PhasedReportEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new PhasedReport
        {
            Id = entity.Id,
            BreachId = entity.BreachId,
            ReportNumber = entity.ReportNumber,
            Content = entity.Content,
            SubmittedAtUtc = entity.SubmittedAtUtc,
            SubmittedByUserId = entity.SubmittedByUserId
        };
    }
}
