using Encina.Compliance.DPIA.Model;

using LanguageExt;

namespace Encina.Compliance.DPIA;

/// <summary>
/// Store for persisting and retrieving DPIA assessment records.
/// </summary>
/// <remarks>
/// <para>
/// The DPIA store manages <see cref="DPIAAssessment"/> instances throughout
/// the assessment lifecycle: from draft creation through approval, periodic review,
/// and eventual expiration.
/// </para>
/// <para>
/// Per GDPR Article 35(11), "the controller shall carry out a review to assess if
/// processing is performed in accordance with the data protection impact assessment
/// at least when there is a change of the risk represented by processing operations."
/// The <see cref="GetExpiredAssessmentsAsync"/> method supports this periodic review
/// requirement by identifying assessments past their review date.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store assessments in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Save a new assessment
/// var assessment = new DPIAAssessment
/// {
///     Id = Guid.NewGuid(),
///     RequestTypeName = typeof(ProcessBiometricDataCommand).FullName!,
///     Status = DPIAAssessmentStatus.Draft,
///     CreatedAtUtc = DateTimeOffset.UtcNow
/// };
/// await store.SaveAssessmentAsync(assessment, ct);
///
/// // Check for an existing assessment
/// var existing = await store.GetAssessmentAsync(typeof(ProcessBiometricDataCommand).FullName!, ct);
/// </code>
/// </example>
public interface IDPIAStore
{
    /// <summary>
    /// Saves a new or updated DPIA assessment.
    /// </summary>
    /// <param name="assessment">The assessment to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the assessment
    /// could not be stored.
    /// </returns>
    /// <remarks>
    /// If an assessment with the same <see cref="DPIAAssessment.Id"/> already exists,
    /// it is overwritten (upsert semantics).
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> SaveAssessmentAsync(
        DPIAAssessment assessment,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the most recent DPIA assessment for the specified request type name.
    /// </summary>
    /// <param name="requestTypeName">
    /// The fully-qualified type name of the request to look up.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{DPIAAssessment}"/> containing the assessment if found;
    /// <see cref="LanguageExt.Prelude.None"/> if no assessment exists for the request type;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// This is the primary lookup used by the pipeline behavior to check if a current,
    /// valid assessment exists before allowing processing to proceed.
    /// </remarks>
    ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentAsync(
        string requestTypeName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a DPIA assessment by its unique identifier.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{DPIAAssessment}"/> containing the assessment if found;
    /// <see cref="LanguageExt.Prelude.None"/> if no assessment exists with the given ID;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<DPIAAssessment>>> GetAssessmentByIdAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all assessments whose review date has passed.
    /// </summary>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="DPIAAssessment.NextReviewAtUtc"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of expired assessments, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no assessments have expired.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per GDPR Article 35(11), assessments must be reviewed periodically. This method
    /// identifies assessments past their <see cref="DPIAAssessment.NextReviewAtUtc"/> date
    /// that need re-evaluation.
    /// </para>
    /// <para>
    /// Typically called by a background service (<c>DPIAExpirationMonitorService</c>) on
    /// a regular interval to detect and publish <see cref="DPIAAssessmentExpired"/>
    /// notifications.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetExpiredAssessmentsAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all DPIA assessments.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all assessments, or an <see cref="EncinaError"/> on failure.
    /// Returns an empty list if no assessments exist.
    /// </returns>
    /// <remarks>
    /// Primarily used for compliance reporting, administrative dashboards, and regulatory audits.
    /// For large datasets, consider implementing pagination in provider-specific extensions.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DPIAAssessment>>> GetAllAssessmentsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a DPIA assessment by its unique identifier.
    /// </summary>
    /// <param name="assessmentId">The unique identifier of the assessment to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the assessment
    /// was not found or the deletion failed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Caution:</b> Deleting assessments may have compliance implications. Per Article 5(2)
    /// (accountability principle), controllers must be able to demonstrate compliance.
    /// Consider using status transitions (e.g., to <see cref="DPIAAssessmentStatus.Expired"/>)
    /// instead of deletion for production systems.
    /// </para>
    /// <para>
    /// This method is primarily intended for administrative cleanup and testing scenarios.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> DeleteAssessmentAsync(
        Guid assessmentId,
        CancellationToken cancellationToken = default);
}
