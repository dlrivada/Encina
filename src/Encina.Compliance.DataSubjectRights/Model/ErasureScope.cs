namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Defines the scope of an erasure operation, allowing fine-grained control over
/// which data categories and fields are targeted.
/// </summary>
/// <remarks>
/// <para>
/// An erasure scope narrows the erasure to specific categories or fields rather than
/// erasing all personal data for the subject. This supports scenarios where the data
/// subject requests partial erasure or where specific exemptions apply.
/// </para>
/// <para>
/// If neither <see cref="Categories"/> nor <see cref="SpecificFields"/> is set,
/// the erasure targets all erasable personal data for the subject.
/// </para>
/// </remarks>
public sealed record ErasureScope
{
    /// <summary>
    /// Restrict erasure to specific personal data categories.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means no category restriction — all categories are eligible.
    /// </remarks>
    public IReadOnlyList<PersonalDataCategory>? Categories { get; init; }

    /// <summary>
    /// Restrict erasure to specific field names.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means no field restriction — all fields in the target categories are eligible.
    /// </remarks>
    public IReadOnlyList<string>? SpecificFields { get; init; }

    /// <summary>
    /// The legal ground for the erasure as stated by the data subject.
    /// </summary>
    /// <remarks>
    /// Must correspond to one of the six grounds in Article 17(1)(a-f).
    /// </remarks>
    public required ErasureReason Reason { get; init; }

    /// <summary>
    /// Exemptions to consider during the erasure operation.
    /// </summary>
    /// <remarks>
    /// <c>null</c> means the executor will automatically detect applicable exemptions.
    /// Explicit exemptions override automatic detection when provided.
    /// </remarks>
    public IReadOnlyList<ErasureExemption>? ExemptionsToApply { get; init; }
}
