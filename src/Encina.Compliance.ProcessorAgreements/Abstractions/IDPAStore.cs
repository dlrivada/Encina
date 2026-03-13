using Encina.Compliance.ProcessorAgreements.Model;

using LanguageExt;

namespace Encina.Compliance.ProcessorAgreements;

/// <summary>
/// Store for persisting and retrieving Data Processing Agreement records.
/// </summary>
/// <remarks>
/// <para>
/// The DPA store manages <see cref="DataProcessingAgreement"/> entities — the temporal,
/// contractual records that govern the relationship between controllers and processors.
/// Per GDPR Article 28(3), "processing by a processor shall be governed by a contract
/// or other legal act [...] that is binding on the processor."
/// </para>
/// <para>
/// This interface manages contractual state only. Processor identity and hierarchy
/// is managed separately by <see cref="IProcessorRegistry"/>, following the design
/// principle of separating identity from contractual state (DC 2).
/// </para>
/// <para>
/// A processor may have multiple DPAs over time (renewal, renegotiation), but at most
/// one active DPA at any given time. Use <see cref="GetActiveByProcessorIdAsync"/> to
/// retrieve the current active agreement, or <see cref="GetByProcessorIdAsync"/> to
/// retrieve the full DPA history for a processor.
/// </para>
/// <para>
/// All methods follow Railway Oriented Programming (ROP) using <c>Either&lt;EncinaError, T&gt;</c>
/// to provide explicit error handling without exceptions for business logic.
/// </para>
/// <para>
/// Implementations may store agreements in-memory (for development/testing), in a database
/// (for production), or in any other suitable backing store. All 13 database providers
/// are supported: ADO.NET (4), Dapper (4), EF Core (4), and MongoDB (1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Add a new DPA for a processor
/// var dpa = new DataProcessingAgreement
/// {
///     Id = "dpa-stripe-2026",
///     ProcessorId = "stripe-payments",
///     Status = DPAStatus.Active,
///     SignedAtUtc = DateTimeOffset.UtcNow,
///     ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
///     MandatoryTerms = new DPAMandatoryTerms { /* all true */ },
///     HasSCCs = true,
///     ProcessingPurposes = ["Payment processing"],
///     CreatedAtUtc = DateTimeOffset.UtcNow,
///     LastUpdatedAtUtc = DateTimeOffset.UtcNow
/// };
/// await store.AddAsync(dpa, ct);
///
/// // Check for expiring agreements
/// var expiring = await store.GetExpiringAsync(DateTimeOffset.UtcNow.AddDays(30), ct);
/// </code>
/// </example>
public interface IDPAStore
{
    /// <summary>
    /// Adds a new Data Processing Agreement to the store.
    /// </summary>
    /// <param name="agreement">The agreement to persist.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the agreement
    /// could not be stored (e.g., duplicate ID).
    /// </returns>
    /// <remarks>
    /// <para>
    /// Per Article 28(3), the contract must set out the subject-matter, duration,
    /// nature and purpose of the processing, the type of personal data, and the
    /// obligations and rights of the controller. The <see cref="DataProcessingAgreement.MandatoryTerms"/>
    /// property tracks compliance with these requirements.
    /// </para>
    /// <para>
    /// Implementations should enforce that at most one DPA per processor has
    /// <see cref="DPAStatus.Active"/> status at any given time.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> AddAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a Data Processing Agreement by its unique identifier.
    /// </summary>
    /// <param name="dpaId">The unique identifier of the agreement.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{DataProcessingAgreement}"/> containing the agreement if found;
    /// <c>None</c> if no agreement exists with the given ID;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetByIdAsync(
        string dpaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all Data Processing Agreements for a specific processor (full history).
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of all agreements (active, expired, terminated) for the processor,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if no agreements exist.
    /// </returns>
    /// <remarks>
    /// Returns the complete DPA history for a processor, including expired and terminated
    /// agreements. This supports compliance audits and the accountability principle
    /// under Article 5(2). For the current active agreement only, use
    /// <see cref="GetActiveByProcessorIdAsync"/>.
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current active Data Processing Agreement for a specific processor.
    /// </summary>
    /// <param name="processorId">The identifier of the processor.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Option{DataProcessingAgreement}"/> containing the active agreement if one exists;
    /// <c>None</c> if the processor has no active DPA;
    /// or an <see cref="EncinaError"/> on failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A processor may have at most one active DPA at any given time. This method returns
    /// the agreement with <see cref="DPAStatus.Active"/> status for the specified processor.
    /// </para>
    /// <para>
    /// This is the primary lookup used by <see cref="IDPAValidator.HasValidDPAAsync"/> and
    /// the <c>ProcessorValidationPipelineBehavior</c> to determine whether a processor has
    /// a valid contractual basis for processing operations per Article 28(3).
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, Option<DataProcessingAgreement>>> GetActiveByProcessorIdAsync(
        string processorId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing Data Processing Agreement in the store.
    /// </summary>
    /// <param name="agreement">The agreement with updated values.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// <see cref="Unit"/> on success, or an <see cref="EncinaError"/> if the agreement
    /// is not found (<c>processor.dpa_not_found</c>) or the update fails.
    /// </returns>
    /// <remarks>
    /// The agreement is matched by <see cref="DataProcessingAgreement.Id"/>. All mutable
    /// fields are overwritten. Status transitions (e.g., Active → Terminated) should be
    /// validated by the caller before updating.
    /// </remarks>
    ValueTask<Either<EncinaError, Unit>> UpdateAsync(
        DataProcessingAgreement agreement,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all Data Processing Agreements with a specific status.
    /// </summary>
    /// <param name="status">The status to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of agreements matching the given status,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if none match.
    /// </returns>
    /// <remarks>
    /// Useful for compliance reporting (e.g., "list all expired agreements") and
    /// bulk operations (e.g., "notify all processors with pending renewal").
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetByStatusAsync(
        DPAStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves active Data Processing Agreements that will expire before the given threshold.
    /// </summary>
    /// <param name="threshold">
    /// The UTC point in time to check against. Agreements with
    /// <see cref="DataProcessingAgreement.ExpiresAtUtc"/> less than or equal to this value
    /// and <see cref="DataProcessingAgreement.Status"/> equal to <see cref="DPAStatus.Active"/>
    /// are returned.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A read-only list of agreements expiring before the threshold,
    /// or an <see cref="EncinaError"/> on failure. Returns an empty list if none are expiring.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Typically called by the <c>CheckDPAExpirationHandler</c> to detect agreements
    /// approaching expiration and publish <c>DPAExpiringNotification</c> events. Only
    /// returns agreements with <see cref="DPAStatus.Active"/> status and a non-null
    /// <see cref="DataProcessingAgreement.ExpiresAtUtc"/>.
    /// </para>
    /// <para>
    /// For example, to find agreements expiring in the next 30 days:
    /// <c>await store.GetExpiringAsync(DateTimeOffset.UtcNow.AddDays(30), ct)</c>.
    /// </para>
    /// </remarks>
    ValueTask<Either<EncinaError, IReadOnlyList<DataProcessingAgreement>>> GetExpiringAsync(
        DateTimeOffset threshold,
        CancellationToken cancellationToken = default);
}
