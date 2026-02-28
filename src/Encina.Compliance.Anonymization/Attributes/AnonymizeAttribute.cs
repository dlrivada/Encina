using Encina.Compliance.Anonymization.Model;

namespace Encina.Compliance.Anonymization;

/// <summary>
/// Marks a response property for automatic anonymization by the
/// <see cref="AnonymizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// <para>
/// When the pipeline behavior detects this attribute on a <c>TResponse</c> property,
/// it applies the specified <see cref="AnonymizationTechnique"/> to the property value before
/// the response is returned to the caller. The handler works with real data; anonymization
/// occurs on the way out (response-side transformation per GDPR Article 25 — Data Protection by Design).
/// </para>
/// <para>
/// Attribute-level parameters override the defaults configured in <c>AnonymizationOptions</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public record CustomerResponse
/// {
///     public Guid Id { get; init; }
///
///     [Anonymize(Technique = AnonymizationTechnique.DataMasking)]
///     public string FullName { get; init; } = string.Empty;
///
///     [Anonymize(Technique = AnonymizationTechnique.Generalization, Granularity = 10)]
///     public int Age { get; init; }
///
///     [Anonymize(Technique = AnonymizationTechnique.Suppression)]
///     public string PhoneNumber { get; init; } = string.Empty;
///
///     [Anonymize(Technique = AnonymizationTechnique.DataMasking, Pattern = "****@{domain}")]
///     public string Email { get; init; } = string.Empty;
///
///     [Anonymize(Technique = AnonymizationTechnique.Perturbation, NoiseRange = 0.2)]
///     public decimal Salary { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class AnonymizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the anonymization technique to apply to the property value.
    /// </summary>
    /// <remarks>
    /// Determines which <see cref="IAnonymizationTechnique"/> implementation processes the field.
    /// Available techniques include generalization, suppression, perturbation, data masking,
    /// and swapping. Defaults to <see cref="AnonymizationTechnique.DataMasking"/>.
    /// </remarks>
    public AnonymizationTechnique Technique { get; set; } = AnonymizationTechnique.DataMasking;

    /// <summary>
    /// Gets or sets the granularity level for generalization (e.g., rounding to nearest 10, 100).
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Technique"/> is <see cref="AnonymizationTechnique.Generalization"/>.
    /// For numeric values, specifies the rounding factor. For dates, specifies the truncation level
    /// (e.g., 1 = year, 2 = month). When <c>null</c>, the technique's default granularity is used.
    /// </remarks>
    public int? Granularity { get; set; }

    /// <summary>
    /// Gets or sets a masking pattern for data masking (e.g., preserving email domain).
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Technique"/> is <see cref="AnonymizationTechnique.DataMasking"/>.
    /// The pattern controls which portions of the value are preserved. When <c>null</c>,
    /// the default masking behavior is used (preserve first character, mask the rest).
    /// </remarks>
    public string? Pattern { get; set; }

    /// <summary>
    /// Gets or sets the noise range for perturbation as a fraction of the original value (0.0-1.0).
    /// </summary>
    /// <remarks>
    /// Only applicable when <see cref="Technique"/> is <see cref="AnonymizationTechnique.Perturbation"/>.
    /// For example, a noise range of <c>0.1</c> applied to value <c>100</c> produces a value
    /// between <c>90</c> and <c>110</c>. When <c>null</c>, the default noise range (0.1) is used.
    /// </remarks>
    public double? NoiseRange { get; set; }
}
