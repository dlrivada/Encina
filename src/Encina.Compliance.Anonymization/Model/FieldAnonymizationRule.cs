namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// Defines how a specific field should be anonymized within an <see cref="AnonymizationProfile"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each rule maps a field name to an anonymization technique with optional technique-specific
/// parameters. Rules are evaluated when the <see cref="AnonymizationProfile"/> is applied
/// to a data object, transforming only the fields with matching rules.
/// </para>
/// <para>
/// Supported parameter keys vary by technique:
/// <list type="table">
/// <listheader>
/// <term>Technique</term>
/// <description>Parameter keys</description>
/// </listheader>
/// <item>
/// <term><see cref="AnonymizationTechnique.Generalization"/></term>
/// <description><c>"Granularity"</c> (int) — range size for numeric values</description>
/// </item>
/// <item>
/// <term><see cref="AnonymizationTechnique.Perturbation"/></term>
/// <description><c>"NoiseRange"</c> (double) — maximum deviation as a fraction (0.0-1.0)</description>
/// </item>
/// <item>
/// <term><see cref="AnonymizationTechnique.DataMasking"/></term>
/// <description><c>"Pattern"</c> (string) — masking pattern using <c>*</c> as mask character</description>
/// </item>
/// <item>
/// <term><see cref="AnonymizationTechnique.KAnonymity"/></term>
/// <description><c>"K"</c> (int) — minimum group size; <c>"QuasiIdentifiers"</c> (string[]) — field names</description>
/// </item>
/// <item>
/// <term><see cref="AnonymizationTechnique.LDiversity"/></term>
/// <description><c>"L"</c> (int) — minimum distinct values; <c>"SensitiveAttribute"</c> (string)</description>
/// </item>
/// <item>
/// <term><see cref="AnonymizationTechnique.TCloseness"/></term>
/// <description><c>"T"</c> (double) — maximum EMD distance; <c>"SensitiveAttribute"</c> (string)</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Generalize age into ranges of 10
/// var ageRule = new FieldAnonymizationRule
/// {
///     FieldName = "Age",
///     Technique = AnonymizationTechnique.Generalization,
///     Parameters = new Dictionary&lt;string, object&gt; { ["Granularity"] = 10 }
/// };
///
/// // Mask email preserving domain
/// var emailRule = new FieldAnonymizationRule
/// {
///     FieldName = "Email",
///     Technique = AnonymizationTechnique.DataMasking,
///     Parameters = new Dictionary&lt;string, object&gt; { ["Pattern"] = "***@{domain}" }
/// };
///
/// // Suppress name entirely
/// var nameRule = new FieldAnonymizationRule
/// {
///     FieldName = "Name",
///     Technique = AnonymizationTechnique.Suppression
/// };
/// </code>
/// </example>
public sealed record FieldAnonymizationRule
{
    /// <summary>
    /// The name of the field (property) to which this anonymization rule applies.
    /// </summary>
    /// <remarks>
    /// Must match the property name on the target data type exactly (case-sensitive).
    /// </remarks>
    public required string FieldName { get; init; }

    /// <summary>
    /// The anonymization technique to apply to this field.
    /// </summary>
    public required AnonymizationTechnique Technique { get; init; }

    /// <summary>
    /// Optional technique-specific parameters controlling the transformation behavior.
    /// </summary>
    /// <remarks>
    /// <c>null</c> when the technique requires no additional configuration
    /// (e.g., <see cref="AnonymizationTechnique.Suppression"/>).
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
