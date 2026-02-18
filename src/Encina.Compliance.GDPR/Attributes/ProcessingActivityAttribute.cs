namespace Encina.Compliance.GDPR;

/// <summary>
/// Declares that a request type is associated with a GDPR processing activity (Article 30).
/// </summary>
/// <remarks>
/// <para>
/// This attribute links an Encina request (command, query, or notification) to its GDPR
/// processing activity documentation. When <c>GDPROptions.AutoRegisterFromAttributes</c> is enabled,
/// activities decorated with this attribute are automatically registered in the
/// <see cref="IProcessingActivityRegistry"/> at startup.
/// </para>
/// <para>
/// The <c>GDPRCompliancePipelineBehavior</c> uses this attribute to:
/// </para>
/// <list type="number">
/// <item>Identify requests that process personal data</item>
/// <item>Verify the processing activity is registered in the RoPA</item>
/// <item>Validate that a lawful basis is declared</item>
/// <item>Log processing for accountability (Article 5(2))</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [ProcessingActivity(
///     Purpose = "Order fulfillment",
///     LawfulBasis = LawfulBasis.Contract,
///     DataCategories = new[] { "Name", "Email", "Address" },
///     DataSubjects = new[] { "Customers" },
///     RetentionDays = 2555)]  // 7 years
/// public record CreateOrderCommand : ICommand&lt;OrderId&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class ProcessingActivityAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the purpose of the processing activity.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(b). Should clearly describe why personal data is processed.
    /// </remarks>
    /// <example>"Fulfill customer orders and deliver purchased goods"</example>
    public required string Purpose { get; set; }

    /// <summary>
    /// Gets or sets the lawful basis for processing under Article 6(1).
    /// </summary>
    /// <remarks>
    /// One of the six lawful bases must be specified. The chosen basis should reflect
    /// the actual legal ground for the processing.
    /// </remarks>
    public required LawfulBasis LawfulBasis { get; set; }

    /// <summary>
    /// Gets or sets the categories of personal data processed.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(c). Examples: "Name", "Email", "IP Address", "Payment Information".
    /// </remarks>
    public required string[] DataCategories { get; set; }

    /// <summary>
    /// Gets or sets the categories of data subjects.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(c). Examples: "Customers", "Employees", "Website Visitors".
    /// </remarks>
    public required string[] DataSubjects { get; set; }

    /// <summary>
    /// Gets or sets the data retention period in days.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(f). Represents the envisaged time limits for erasure
    /// of the different categories of data. Use <c>0</c> if retention is indefinite
    /// (must be justified by lawful basis).
    /// </remarks>
    /// <example>2555 (approximately 7 years)</example>
    public required int RetentionDays { get; set; }

    /// <summary>
    /// Gets or sets the categories of recipients to whom personal data is disclosed.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(d). Defaults to an empty array if no recipients.
    /// </remarks>
    public string[] Recipients { get; set; } = [];

    /// <summary>
    /// Gets or sets the description of technical and organizational security measures.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(g) where possible. Defaults to an empty string.
    /// </remarks>
    public string SecurityMeasures { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of transfers to third countries.
    /// </summary>
    /// <remarks>
    /// Required by Article 30(1)(e) when applicable. <c>null</c> if no transfers occur.
    /// </remarks>
    public string? ThirdCountryTransfers { get; set; }

    /// <summary>
    /// Gets or sets the safeguards for third-country transfers (Article 46).
    /// </summary>
    /// <remarks>
    /// Examples: "Standard Contractual Clauses (SCCs)", "Adequacy Decision", "Binding Corporate Rules (BCRs)".
    /// </remarks>
    public string? Safeguards { get; set; }
}
