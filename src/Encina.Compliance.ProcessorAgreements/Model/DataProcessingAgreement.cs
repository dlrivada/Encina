namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// Represents a Data Processing Agreement between a controller and a processor.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 28(3) requires that "processing by a processor shall be governed by a contract
/// or other legal act [...] that sets out the subject-matter and duration of the processing,
/// the nature and purpose of the processing, the type of personal data and categories of data
/// subjects and the obligations and rights of the controller."
/// </para>
/// <para>
/// This is a temporal entity representing the contractual state of a processor relationship.
/// A processor (<see cref="Processor"/>) may have multiple agreements over time (renewal,
/// renegotiation). The <see cref="Status"/> tracks the lifecycle:
/// <c>Active → PendingRenewal → Active</c> (renewal), or
/// <c>Active → Expired</c> (lapsed), or
/// <c>Active → Terminated</c> (explicit termination).
/// </para>
/// <para>
/// The <see cref="MandatoryTerms"/> property tracks compliance with the eight mandatory
/// contractual clauses defined in Article 28(3)(a)-(h).
/// </para>
/// </remarks>
public sealed record DataProcessingAgreement
{
    /// <summary>
    /// Unique identifier for this agreement.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The identifier of the processor this agreement covers.
    /// </summary>
    /// <remarks>
    /// References <see cref="Processor.Id"/>. A processor must be registered in the
    /// <c>IProcessorRegistry</c> before an agreement can be created.
    /// </remarks>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// The current lifecycle status of this agreement.
    /// </summary>
    public required DPAStatus Status { get; init; }

    /// <summary>
    /// The UTC timestamp when this agreement was signed by both parties.
    /// </summary>
    public required DateTimeOffset SignedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when this agreement expires, or <see langword="null"/> for indefinite agreements.
    /// </summary>
    /// <remarks>
    /// When set, the expiration monitoring system publishes
    /// <see cref="Notifications.DPAExpiringNotification"/> as the date approaches
    /// and <see cref="Notifications.DPAExpiredNotification"/> when it passes.
    /// </remarks>
    public DateTimeOffset? ExpiresAtUtc { get; init; }

    /// <summary>
    /// The compliance status of the eight mandatory contractual terms per Article 28(3).
    /// </summary>
    public required DPAMandatoryTerms MandatoryTerms { get; init; }

    /// <summary>
    /// Whether Standard Contractual Clauses are included in this agreement.
    /// </summary>
    /// <remarks>
    /// SCCs are required for cross-border data transfers to countries without an
    /// adequacy decision per Articles 46(2)(c) and 46(2)(d).
    /// </remarks>
    public required bool HasSCCs { get; init; }

    /// <summary>
    /// The documented processing purposes covered by this agreement.
    /// </summary>
    /// <remarks>
    /// Per Article 28(3), the contract must set out "the nature and purpose of the processing."
    /// </remarks>
    public required IReadOnlyList<string> ProcessingPurposes { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// The UTC timestamp when this agreement record was created.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when this agreement was last updated.
    /// </summary>
    public required DateTimeOffset LastUpdatedAtUtc { get; init; }

    /// <summary>
    /// Determines whether this agreement is currently active and not expired.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An agreement is active when:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Its <see cref="Status"/> is <see cref="DPAStatus.Active"/>.</description></item>
    /// <item><description>Its <see cref="ExpiresAtUtc"/> has not passed (or is <see langword="null"/>, indicating no expiration).</description></item>
    /// </list>
    /// <para>
    /// The <c>ProcessorValidationPipelineBehavior</c> calls this method to determine whether
    /// a processor has a valid agreement for allowing processing operations to proceed.
    /// </para>
    /// </remarks>
    /// <param name="nowUtc">The current UTC time for comparison against <see cref="ExpiresAtUtc"/>.</param>
    /// <returns><see langword="true"/> if the agreement is active and has not expired; otherwise, <see langword="false"/>.</returns>
    public bool IsActive(DateTimeOffset nowUtc) =>
        Status == DPAStatus.Active &&
        (ExpiresAtUtc is null || ExpiresAtUtc > nowUtc);
}
