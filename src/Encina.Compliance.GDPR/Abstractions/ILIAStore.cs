using LanguageExt;

namespace Encina.Compliance.GDPR;

/// <summary>
/// Store for managing Legitimate Interest Assessment (LIA) records.
/// </summary>
/// <remarks>
/// <para>
/// LIA records document the three-part test (purpose, necessity, balancing) performed
/// under Article 6(1)(f). This store provides persistence operations for LIA records
/// and is used by <see cref="ILegitimateInterestAssessment"/> to validate that a
/// referenced LIA exists and is approved.
/// </para>
/// <para>
/// Implementations may store records in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Store a new LIA record
/// var lia = new LIARecord
/// {
///     Id = "LIA-2024-FRAUD-001",
///     Name = "Fraud Detection LIA",
///     // ... other properties
///     Outcome = LIAOutcome.Approved,
///     Conclusion = "Legitimate interest outweighs impact",
///     AssessedAtUtc = DateTimeOffset.UtcNow,
///     AssessedBy = "DPO"
/// };
/// await store.StoreAsync(lia, cancellationToken);
///
/// // Retrieve by reference
/// var result = await store.GetByReferenceAsync("LIA-2024-FRAUD-001", cancellationToken);
/// </code>
/// </example>
public interface ILIAStore
{
    /// <summary>
    /// Stores a new LIA record or updates an existing one with the same <see cref="LIARecord.Id"/>.
    /// </summary>
    /// <param name="record">The LIA record to store.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the record could not be stored.
    /// </returns>
    ValueTask<Either<EncinaError, Unit>> StoreAsync(
        LIARecord record,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a LIA record by its reference identifier.
    /// </summary>
    /// <param name="liaReference">The LIA reference identifier (maps to <see cref="LIARecord.Id"/>).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// An <see cref="Option{LIARecord}"/> containing the matching record if found,
    /// or <see cref="Option{LIARecord}.None"/> if no record exists for the given reference,
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<LIARecord>>> GetByReferenceAsync(
        string liaReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all LIA records that require review (outcome is <see cref="LIAOutcome.RequiresReview"/>).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of LIA records pending review, or an <see cref="EncinaError"/>
    /// if the store could not be queried.
    /// </returns>
    /// <remarks>
    /// This method is typically used by governance dashboards and DPO review workflows.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<LIARecord>>> GetPendingReviewAsync(
        CancellationToken cancellationToken = default);
}
