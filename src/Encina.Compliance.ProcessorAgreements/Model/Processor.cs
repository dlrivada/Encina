namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// Represents a data processor or sub-processor in the processing chain.
/// </summary>
/// <remarks>
/// <para>
/// GDPR Article 28(1) requires that "the controller shall use only processors providing
/// sufficient guarantees to implement appropriate technical and organisational measures."
/// This record captures the identity and hierarchical position of each processor.
/// </para>
/// <para>
/// Processors form a bounded hierarchy using <see cref="ParentProcessorId"/> and <see cref="Depth"/>:
/// </para>
/// <list type="bullet">
/// <item><description>Top-level processors have <see cref="ParentProcessorId"/> = <see langword="null"/> and <see cref="Depth"/> = 0.</description></item>
/// <item><description>Direct sub-processors have <see cref="Depth"/> = 1 (parent's depth + 1).</description></item>
/// <item><description>Sub-sub-processors have <see cref="Depth"/> = 2, and so on.</description></item>
/// </list>
/// <para>
/// The hierarchy is bounded by <c>MaxSubProcessorDepth</c> in configuration to prevent
/// unbounded processing chains, which would complicate compliance oversight per Article 28(4).
/// </para>
/// <para>
/// This is a long-lived identity entity. Contractual state (agreements, terms, expiration)
/// is tracked separately via <see cref="DataProcessingAgreement"/>.
/// </para>
/// </remarks>
public sealed record Processor
{
    /// <summary>
    /// Unique identifier for this processor.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display name of the processor (e.g., "Stripe", "AWS").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The country where the processor is established.
    /// </summary>
    /// <remarks>
    /// Relevant for determining whether Standard Contractual Clauses are required
    /// for cross-border data transfers per Articles 44-49.
    /// </remarks>
    public required string Country { get; init; }

    /// <summary>
    /// The contact email address for the processor's data protection representative,
    /// or <see langword="null"/> if not provided.
    /// </summary>
    public string? ContactEmail { get; init; }

    /// <summary>
    /// The identifier of the parent processor, or <see langword="null"/> for top-level processors.
    /// </summary>
    /// <remarks>
    /// Per Article 28(2), the processor shall not engage another processor without prior
    /// authorization. This field tracks the hierarchical relationship: a sub-processor
    /// references its parent processor.
    /// </remarks>
    public string? ParentProcessorId { get; init; }

    /// <summary>
    /// The depth of this processor in the sub-processor hierarchy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>0</c> = top-level processor (no parent), <c>1</c> = direct sub-processor,
    /// <c>2</c> = sub-sub-processor, etc. The depth is bounded by <c>MaxSubProcessorDepth</c>
    /// in configuration.
    /// </para>
    /// <para>
    /// Per Article 28(4), sub-processors must meet the same data protection obligations
    /// as the original processor. Tracking depth enables compliance oversight of the
    /// entire processing chain.
    /// </para>
    /// </remarks>
    public required int Depth { get; init; }

    /// <summary>
    /// The type of written authorization granted for sub-processor engagement.
    /// </summary>
    /// <remarks>
    /// Article 28(2) distinguishes between specific and general authorization.
    /// Under general authorization, the processor must inform the controller of
    /// intended changes to give the controller the opportunity to object.
    /// </remarks>
    public required SubProcessorAuthorizationType SubProcessorAuthorizationType { get; init; }

    /// <summary>
    /// The tenant identifier for multi-tenancy support, or <see langword="null"/> when tenancy is not used.
    /// </summary>
    public string? TenantId { get; init; }

    /// <summary>
    /// The module identifier for modular monolith isolation, or <see langword="null"/> when module isolation is not used.
    /// </summary>
    public string? ModuleId { get; init; }

    /// <summary>
    /// The UTC timestamp when this processor was registered.
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// The UTC timestamp when this processor was last updated.
    /// </summary>
    public required DateTimeOffset LastUpdatedAtUtc { get; init; }
}
