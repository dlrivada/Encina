namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Result of a data erasure operation, detailing what was erased, retained, and why.
/// </summary>
/// <remarks>
/// <para>
/// After executing an erasure request (Article 17), this record summarizes the outcome:
/// how many fields were successfully erased, how many were retained (and why),
/// and whether any failures occurred during the process.
/// </para>
/// <para>
/// Retention reasons are documented individually per field via <see cref="RetentionReasons"/>,
/// and applicable Article 17(3) exemptions are listed in <see cref="Exemptions"/>.
/// This information must be communicated to the data subject.
/// </para>
/// </remarks>
public sealed record ErasureResult
{
    /// <summary>
    /// Number of personal data fields successfully erased.
    /// </summary>
    public required int FieldsErased { get; init; }

    /// <summary>
    /// Number of personal data fields retained due to legal obligations or exemptions.
    /// </summary>
    public required int FieldsRetained { get; init; }

    /// <summary>
    /// Number of personal data fields where erasure failed due to errors.
    /// </summary>
    public required int FieldsFailed { get; init; }

    /// <summary>
    /// Details for each field that was retained, including the reason for retention.
    /// </summary>
    public required IReadOnlyList<RetentionDetail> RetentionReasons { get; init; }

    /// <summary>
    /// Article 17(3) exemptions that applied to this erasure operation.
    /// </summary>
    public required IReadOnlyList<ErasureExemption> Exemptions { get; init; }
}
