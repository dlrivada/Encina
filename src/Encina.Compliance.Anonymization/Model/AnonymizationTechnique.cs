namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Anonymization techniques for irreversible data transformation.
/// </summary>
/// <remarks>
/// <para>
/// Each technique provides a different trade-off between data utility and privacy protection.
/// The choice of technique depends on the intended use case (analytics, research, publication)
/// and the acceptable level of re-identification risk.
/// </para>
/// <para>
/// Techniques can be combined via <see cref="AnonymizationProfile"/> to apply different
/// transformations to different fields. For example, suppress names while generalizing ages.
/// </para>
/// <para>
/// Per GDPR Recital 26, data that has been irreversibly anonymized is no longer personal data
/// and falls outside the scope of the regulation.
/// </para>
/// </remarks>
public enum AnonymizationTechnique
{
    /// <summary>
    /// Replace a value with a broader category or range.
    /// </summary>
    /// <remarks>
    /// Examples: age 25 becomes "20-30", ZIP code "12345" becomes "123**",
    /// date "2025-03-15" becomes "2025-Q1". Configure granularity via
    /// <see cref="FieldAnonymizationRule.Parameters"/> with key <c>"Granularity"</c>.
    /// </remarks>
    Generalization = 0,

    /// <summary>
    /// Remove the value entirely, replacing it with <c>null</c> or a default placeholder.
    /// </summary>
    /// <remarks>
    /// The strongest privacy protection but lowest data utility. Suitable for
    /// direct identifiers (name, email, phone) that have no analytical value.
    /// </remarks>
    Suppression = 1,

    /// <summary>
    /// Add controlled random noise to numeric values while preserving statistical properties.
    /// </summary>
    /// <remarks>
    /// The noise range is configurable via <see cref="FieldAnonymizationRule.Parameters"/>
    /// with key <c>"NoiseRange"</c>. For example, a noise range of 5 applied to salary 50000
    /// produces a value between 47500 and 52500. Preserves aggregate statistics (mean, distribution)
    /// while preventing individual identification.
    /// </remarks>
    Perturbation = 2,

    /// <summary>
    /// Exchange values between records within the same dataset.
    /// </summary>
    /// <remarks>
    /// Maintains the overall distribution of values in the dataset while breaking
    /// the link between individuals and their specific values. Requires a dataset
    /// context (not applicable to single-record transformations).
    /// </remarks>
    Swapping = 3,

    /// <summary>
    /// Partially mask values while preserving format and a recognizable portion.
    /// </summary>
    /// <remarks>
    /// Examples: "john@email.com" becomes "j***@email.com", phone "555-1234" becomes "555-****".
    /// Configure the masking pattern via <see cref="FieldAnonymizationRule.Parameters"/>
    /// with key <c>"Pattern"</c>. Useful when partial visibility is needed for
    /// human verification without full disclosure.
    /// </remarks>
    DataMasking = 4,

    /// <summary>
    /// Ensure at least <c>k</c> identical records exist for each combination of quasi-identifiers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// K-anonymity prevents singling out individuals by ensuring each record is indistinguishable
    /// from at least <c>k-1</c> other records based on quasi-identifier attributes
    /// (age, gender, ZIP code, etc.).
    /// </para>
    /// <para>
    /// Configure via <see cref="FieldAnonymizationRule.Parameters"/>: <c>"K"</c> (minimum group size,
    /// default 5) and <c>"QuasiIdentifiers"</c> (field names to consider).
    /// </para>
    /// </remarks>
    KAnonymity = 5,

    /// <summary>
    /// Ensure at least <c>l</c> distinct values for sensitive attributes within each equivalence class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// L-diversity extends k-anonymity by preventing homogeneity attacks. Even if an equivalence
    /// class has <c>k</c> records, if they all share the same sensitive value (e.g., all have
    /// "cancer" as diagnosis), the individual's data is effectively disclosed.
    /// </para>
    /// <para>
    /// Configure via <see cref="FieldAnonymizationRule.Parameters"/>: <c>"L"</c> (minimum distinct
    /// sensitive values per class) and <c>"SensitiveAttribute"</c> (the field to diversify).
    /// </para>
    /// </remarks>
    LDiversity = 6,

    /// <summary>
    /// Limit the distribution distance between sensitive attribute values in each equivalence class
    /// and the overall dataset distribution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// T-closeness addresses the skewness attack on l-diversity. It ensures that the distribution
    /// of a sensitive attribute within any equivalence class is close (within threshold <c>t</c>)
    /// to the distribution in the full dataset, measured using Earth Mover's Distance (EMD).
    /// </para>
    /// <para>
    /// Configure via <see cref="FieldAnonymizationRule.Parameters"/>: <c>"T"</c> (maximum
    /// allowed EMD distance, e.g., 0.15) and <c>"SensitiveAttribute"</c>.
    /// </para>
    /// </remarks>
    TCloseness = 7
}
