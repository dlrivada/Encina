namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Result summary of an anonymization operation applied to a data object.
/// </summary>
/// <remarks>
/// <para>
/// After applying an <see cref="AnonymizationProfile"/> to a data object, this result
/// describes what happened: how many fields were transformed, how many were skipped
/// (no matching rule or unsupported type), and which technique was applied to each field.
/// </para>
/// <para>
/// This information is useful for audit logging, compliance reporting, and verifying
/// that the expected transformations were applied.
/// </para>
/// </remarks>
public sealed record AnonymizationResult
{
    /// <summary>
    /// Total number of fields present in the target data object.
    /// </summary>
    public required int OriginalFieldCount { get; init; }

    /// <summary>
    /// Number of fields that were successfully anonymized.
    /// </summary>
    public required int AnonymizedFieldCount { get; init; }

    /// <summary>
    /// Number of fields that were skipped (no matching rule, unsupported type, or exempt).
    /// </summary>
    public required int SkippedFieldCount { get; init; }

    /// <summary>
    /// Maps each anonymized field name to the technique that was applied.
    /// </summary>
    /// <remarks>
    /// Only contains entries for fields that were successfully transformed.
    /// Fields that were skipped do not appear in this dictionary.
    /// </remarks>
    public required IReadOnlyDictionary<string, AnonymizationTechnique> TechniqueApplied { get; init; }
}
