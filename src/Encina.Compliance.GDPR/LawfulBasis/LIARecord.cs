namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents a Legitimate Interest Assessment (LIA) record following the EDPB three-part test.
/// </summary>
/// <remarks>
/// <para>
/// Article 6(1)(f) allows processing based on legitimate interests, provided those interests
/// are not overridden by the data subject's fundamental rights and freedoms. A LIA documents
/// the three-part test recommended by the European Data Protection Board (EDPB):
/// </para>
/// <list type="number">
/// <item><b>Purpose Test</b>: Is the interest legitimate? (Fields: <see cref="LegitimateInterest"/>,
/// <see cref="Benefits"/>, <see cref="ConsequencesIfNotProcessed"/>)</item>
/// <item><b>Necessity Test</b>: Is the processing necessary for that interest? (Fields:
/// <see cref="NecessityJustification"/>, <see cref="AlternativesConsidered"/>,
/// <see cref="DataMinimisationNotes"/>)</item>
/// <item><b>Balancing Test</b>: Do the individual's rights override the interest? (Fields:
/// <see cref="NatureOfData"/>, <see cref="ReasonableExpectations"/>,
/// <see cref="ImpactAssessment"/>, <see cref="Safeguards"/>)</item>
/// </list>
/// <para>
/// The <see cref="Id"/> property maps to <see cref="LawfulBasisRegistration.LIAReference"/>
/// and <see cref="LawfulBasisAttribute.LIAReference"/>, linking the assessment to specific
/// request types that claim legitimate interests as their lawful basis.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var lia = new LIARecord
/// {
///     Id = "LIA-2024-FRAUD-001",
///     Name = "Fraud Detection LIA",
///     Purpose = "Detect and prevent fraudulent transactions",
///     LegitimateInterest = "Protect financial integrity and prevent losses",
///     Benefits = "Reduced fraud, lower chargebacks, safer transactions",
///     ConsequencesIfNotProcessed = "Significant financial losses and increased fraud risk",
///     NecessityJustification = "Real-time analysis is essential for fraud prevention",
///     AlternativesConsidered = ["Manual review", "Post-hoc analysis"],
///     DataMinimisationNotes = "Only transaction metadata is analyzed, not personal communications",
///     NatureOfData = "Transaction amounts, timestamps, merchant categories",
///     ReasonableExpectations = "Customers expect their bank to protect against fraud",
///     ImpactAssessment = "Minimal impact on data subjects; benefits significantly outweigh risks",
///     Safeguards = ["Automated alerts only", "Human review before account action", "Data encrypted at rest"],
///     Outcome = LIAOutcome.Approved,
///     Conclusion = "Legitimate interest in fraud prevention outweighs the minimal impact on data subjects",
///     AssessedBy = "Data Protection Officer",
///     AssessedAtUtc = DateTimeOffset.UtcNow,
///     NextReviewAtUtc = DateTimeOffset.UtcNow.AddYears(1)
/// };
/// </code>
/// </example>
public sealed record LIARecord
{
    // --- Identification ---

    /// <summary>
    /// Unique identifier for this LIA record.
    /// </summary>
    /// <remarks>
    /// Maps to <see cref="LawfulBasisRegistration.LIAReference"/> and
    /// <see cref="LawfulBasisAttribute.LIAReference"/>. Typically a document reference
    /// (e.g., "LIA-2024-FRAUD-001").
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name describing this LIA.
    /// </summary>
    /// <example>"Fraud Detection LIA", "Marketing Analytics LIA"</example>
    public required string Name { get; init; }

    /// <summary>
    /// The processing purpose this LIA covers.
    /// </summary>
    /// <example>"Detect and prevent fraudulent transactions"</example>
    public required string Purpose { get; init; }

    // --- Purpose Test ---

    /// <summary>
    /// Description of the legitimate interest being pursued.
    /// </summary>
    /// <remarks>
    /// Part 1 of the EDPB three-part test. Must identify a specific, concrete interest
    /// that is lawful and clearly articulated.
    /// </remarks>
    public required string LegitimateInterest { get; init; }

    /// <summary>
    /// The benefits of the processing to the controller, data subject, or third parties.
    /// </summary>
    public required string Benefits { get; init; }

    /// <summary>
    /// The consequences of not carrying out the processing.
    /// </summary>
    public required string ConsequencesIfNotProcessed { get; init; }

    // --- Necessity Test ---

    /// <summary>
    /// Justification for why the processing is necessary for the legitimate interest.
    /// </summary>
    /// <remarks>
    /// Part 2 of the EDPB three-part test. Must demonstrate that the processing is
    /// proportionate and no less intrusive means are available.
    /// </remarks>
    public required string NecessityJustification { get; init; }

    /// <summary>
    /// Alternative approaches that were considered before choosing this processing.
    /// </summary>
    /// <remarks>
    /// Demonstrates compliance with the proportionality principle by showing that
    /// less intrusive alternatives were evaluated and found insufficient.
    /// </remarks>
    public required IReadOnlyList<string> AlternativesConsidered { get; init; }

    /// <summary>
    /// Notes on data minimisation measures applied to the processing.
    /// </summary>
    public required string DataMinimisationNotes { get; init; }

    // --- Balancing Test ---

    /// <summary>
    /// Description of the nature of the personal data being processed.
    /// </summary>
    /// <remarks>
    /// Part 3 of the EDPB three-part test. More sensitive data types (e.g., health, financial)
    /// carry greater weight for the data subject's rights.
    /// </remarks>
    public required string NatureOfData { get; init; }

    /// <summary>
    /// Assessment of the data subject's reasonable expectations regarding the processing.
    /// </summary>
    public required string ReasonableExpectations { get; init; }

    /// <summary>
    /// Assessment of the impact on data subjects' rights and freedoms.
    /// </summary>
    public required string ImpactAssessment { get; init; }

    /// <summary>
    /// Safeguards implemented to mitigate the impact on data subjects.
    /// </summary>
    /// <remarks>
    /// Examples: pseudonymisation, encryption, access controls, opt-out mechanisms,
    /// transparency measures, human review before automated decisions.
    /// </remarks>
    public required IReadOnlyList<string> Safeguards { get; init; }

    // --- Outcome ---

    /// <summary>
    /// The outcome of the LIA assessment.
    /// </summary>
    public required LIAOutcome Outcome { get; init; }

    /// <summary>
    /// Summary conclusion of the assessment.
    /// </summary>
    public required string Conclusion { get; init; }

    /// <summary>
    /// Any conditions attached to the approval (e.g., "only for EU transactions").
    /// </summary>
    public string? Conditions { get; init; }

    // --- Governance ---

    /// <summary>
    /// Timestamp when the assessment was conducted (UTC).
    /// </summary>
    public required DateTimeOffset AssessedAtUtc { get; init; }

    /// <summary>
    /// Name or role of the person who conducted the assessment.
    /// </summary>
    /// <example>"Data Protection Officer", "Privacy Counsel"</example>
    public required string AssessedBy { get; init; }

    /// <summary>
    /// Whether the DPO was involved in or consulted during the assessment.
    /// </summary>
    public bool DPOInvolvement { get; init; }

    /// <summary>
    /// Timestamp when the next periodic review of this LIA is due (UTC).
    /// </summary>
    /// <remarks>
    /// LIAs should be reviewed periodically to ensure the assessment remains valid
    /// as circumstances change.
    /// </remarks>
    public DateTimeOffset? NextReviewAtUtc { get; init; }
}
