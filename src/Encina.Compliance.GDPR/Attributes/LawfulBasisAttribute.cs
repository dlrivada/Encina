namespace Encina.Compliance.GDPR;

/// <summary>
/// Declares the lawful basis for processing personal data associated with a request type (Article 6).
/// </summary>
/// <remarks>
/// <para>
/// This attribute links an Encina request (command, query, or notification) to a specific
/// lawful basis under GDPR Article 6(1). It can be used independently or alongside
/// <see cref="ProcessingActivityAttribute"/> for lightweight lawful basis declarations.
/// </para>
/// <para>
/// When <c>LawfulBasisOptions.AutoRegisterFromAttributes</c> is enabled, requests decorated with
/// this attribute are automatically registered in the <see cref="ILawfulBasisRegistry"/> at startup.
/// </para>
/// <para>
/// For basis-specific metadata, use the optional properties:
/// </para>
/// <list type="bullet">
/// <item><see cref="Purpose"/>: describes the processing purpose (recommended for all bases)</item>
/// <item><see cref="LIAReference"/>: reference to a Legitimate Interest Assessment (required for <see cref="LawfulBasis.LegitimateInterests"/>)</item>
/// <item><see cref="LegalReference"/>: reference to the specific legal provision (for <see cref="LawfulBasis.LegalObligation"/>)</item>
/// <item><see cref="ContractReference"/>: reference to the contract or pre-contractual steps (for <see cref="LawfulBasis.Contract"/>)</item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Consent-based processing
/// [LawfulBasis(LawfulBasis.Consent, Purpose = "Send marketing newsletters")]
/// public record SendNewsletterCommand : ICommand;
///
/// // Contract-based processing
/// [LawfulBasis(LawfulBasis.Contract,
///     Purpose = "Fulfill customer orders",
///     ContractReference = "Terms of Service v2.1")]
/// public record CreateOrderCommand : ICommand&lt;OrderId&gt;;
///
/// // Legitimate interests with LIA reference
/// [LawfulBasis(LawfulBasis.LegitimateInterests,
///     Purpose = "Fraud detection and prevention",
///     LIAReference = "LIA-2024-FRAUD-001")]
/// public record AnalyzeTransactionCommand : ICommand&lt;FraudScore&gt;;
///
/// // Legal obligation
/// [LawfulBasis(LawfulBasis.LegalObligation,
///     Purpose = "Tax reporting compliance",
///     LegalReference = "EU VAT Directive 2006/112/EC")]
/// public record GenerateTaxReportCommand : ICommand&lt;TaxReport&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class LawfulBasisAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LawfulBasisAttribute"/> class.
    /// </summary>
    /// <param name="basis">The lawful basis under Article 6(1) for this processing.</param>
    public LawfulBasisAttribute(LawfulBasis basis)
    {
        Basis = basis;
    }

    /// <summary>
    /// Gets the lawful basis for processing under Article 6(1).
    /// </summary>
    public LawfulBasis Basis { get; }

    /// <summary>
    /// Gets or sets the purpose of the processing.
    /// </summary>
    /// <remarks>
    /// Recommended for all lawful bases. Describes why personal data is processed
    /// under this specific legal ground.
    /// </remarks>
    /// <example>"Fraud detection and prevention"</example>
    public string? Purpose { get; init; }

    /// <summary>
    /// Gets or sets the reference to a Legitimate Interest Assessment (LIA).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required when <see cref="Basis"/> is <see cref="LawfulBasis.LegitimateInterests"/>.
    /// The LIA documents the balancing test between the controller's interests and the
    /// data subject's rights and freedoms.
    /// </para>
    /// <para>
    /// Typically a document reference or identifier (e.g., "LIA-2024-FRAUD-001").
    /// </para>
    /// </remarks>
    public string? LIAReference { get; init; }

    /// <summary>
    /// Gets or sets the reference to the specific legal provision.
    /// </summary>
    /// <remarks>
    /// Relevant when <see cref="Basis"/> is <see cref="LawfulBasis.LegalObligation"/>.
    /// Should identify the specific law, regulation, or directive that mandates the processing.
    /// </remarks>
    /// <example>"EU VAT Directive 2006/112/EC"</example>
    public string? LegalReference { get; init; }

    /// <summary>
    /// Gets or sets the reference to the contract or pre-contractual steps.
    /// </summary>
    /// <remarks>
    /// Relevant when <see cref="Basis"/> is <see cref="LawfulBasis.Contract"/>.
    /// Should identify the contract, terms of service, or pre-contractual arrangement
    /// that necessitates the processing.
    /// </remarks>
    /// <example>"Terms of Service v2.1, Section 3.2"</example>
    public string? ContractReference { get; init; }
}
