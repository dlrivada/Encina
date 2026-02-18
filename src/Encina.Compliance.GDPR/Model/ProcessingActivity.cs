namespace Encina.Compliance.GDPR;

/// <summary>
/// Represents a record of processing activity as required by GDPR Article 30.
/// </summary>
/// <remarks>
/// <para>
/// Each processing activity documents what personal data is processed, why, and how.
/// The collection of all processing activities forms the Records of Processing Activities (RoPA),
/// which must be maintained by every controller (and processor) under Article 30.
/// </para>
/// <para>
/// Processing activities are linked to Encina request types via <see cref="RequestType"/>,
/// enabling automatic compliance validation in the pipeline.
/// </para>
/// </remarks>
public sealed record ProcessingActivity
{
    /// <summary>
    /// Unique identifier for this processing activity record.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Human-readable name describing this processing activity.
    /// </summary>
    /// <example>"Customer Order Processing", "Newsletter Subscription"</example>
    public required string Name { get; init; }

    /// <summary>
    /// Purpose of the processing as required by Article 30(1)(b).
    /// </summary>
    /// <example>"Fulfill customer orders and deliver purchased goods"</example>
    public required string Purpose { get; init; }

    /// <summary>
    /// Lawful basis for the processing under Article 6(1).
    /// </summary>
    public required LawfulBasis LawfulBasis { get; init; }

    /// <summary>
    /// Categories of data subjects whose data is processed, as required by Article 30(1)(c).
    /// </summary>
    /// <example>["Customers", "Employees", "Website Visitors"]</example>
    public required IReadOnlyList<string> CategoriesOfDataSubjects { get; init; }

    /// <summary>
    /// Categories of personal data processed, as required by Article 30(1)(c).
    /// </summary>
    /// <example>["Name", "Email", "Address", "Payment Information"]</example>
    public required IReadOnlyList<string> CategoriesOfPersonalData { get; init; }

    /// <summary>
    /// Categories of recipients to whom personal data is disclosed, as required by Article 30(1)(d).
    /// </summary>
    /// <example>["Shipping Provider", "Payment Processor", "Tax Authority"]</example>
    public required IReadOnlyList<string> Recipients { get; init; }

    /// <summary>
    /// Description of transfers to third countries or international organizations, as required by Article 30(1)(e).
    /// </summary>
    /// <remarks>
    /// <c>null</c> if no third-country transfers occur. When transfers exist, document the
    /// countries and applicable safeguards (SCCs, adequacy decisions, BCRs).
    /// </remarks>
    public string? ThirdCountryTransfers { get; init; }

    /// <summary>
    /// Appropriate safeguards for third-country transfers (Article 46).
    /// </summary>
    /// <remarks>
    /// Examples: Standard Contractual Clauses (SCCs), Adequacy Decision, Binding Corporate Rules (BCRs).
    /// </remarks>
    public string? Safeguards { get; init; }

    /// <summary>
    /// Envisaged time limits for erasure of different categories of data, as required by Article 30(1)(f).
    /// </summary>
    public required TimeSpan RetentionPeriod { get; init; }

    /// <summary>
    /// General description of technical and organizational security measures, as required by Article 30(1)(g).
    /// </summary>
    /// <example>"Encryption at rest (AES-256), TLS 1.3 in transit, role-based access control"</example>
    public required string SecurityMeasures { get; init; }

    /// <summary>
    /// The Encina request type linked to this processing activity.
    /// </summary>
    /// <remarks>
    /// This links the GDPR documentation to actual code, enabling the compliance pipeline
    /// behavior to verify that processing activities are registered before execution.
    /// </remarks>
    public required Type RequestType { get; init; }

    /// <summary>
    /// Timestamp when this processing activity record was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Timestamp when this processing activity record was last updated (UTC).
    /// </summary>
    public required DateTimeOffset LastUpdatedAtUtc { get; init; }
}
