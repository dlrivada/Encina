namespace Encina.Compliance.Anonymization.Model;

/// <summary>
/// A named collection of field-level anonymization rules that can be applied as a unit.
/// </summary>
/// <remarks>
/// <para>
/// Profiles enable reusable anonymization configurations. For example, an "analytics" profile
/// might generalize ages and suppress names, while a "research" profile might apply k-anonymity
/// across quasi-identifiers with higher privacy guarantees.
/// </para>
/// <para>
/// Profiles are immutable and can be shared across operations. Use the <see cref="Create"/>
/// factory method to construct new profiles with a generated identifier.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var profile = AnonymizationProfile.Create(
///     name: "analytics-export",
///     description: "Profile for analytics data export — suppress PII, generalize demographics",
///     fieldRules:
///     [
///         new FieldAnonymizationRule
///         {
///             FieldName = "Name",
///             Technique = AnonymizationTechnique.Suppression
///         },
///         new FieldAnonymizationRule
///         {
///             FieldName = "Age",
///             Technique = AnonymizationTechnique.Generalization,
///             Parameters = new Dictionary&lt;string, object&gt; { ["Granularity"] = 10 }
///         },
///         new FieldAnonymizationRule
///         {
///             FieldName = "Email",
///             Technique = AnonymizationTechnique.DataMasking,
///             Parameters = new Dictionary&lt;string, object&gt; { ["Pattern"] = "***@{domain}" }
///         }
///     ]);
/// </code>
/// </example>
public sealed record AnonymizationProfile
{
    /// <summary>
    /// Unique identifier for this anonymization profile.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable name for this profile.
    /// </summary>
    /// <remarks>
    /// Used for identification in configuration, logging, and diagnostics.
    /// Should be descriptive of the profile's purpose (e.g., "analytics-export", "research-dataset").
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description explaining the purpose and scope of this profile.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The field-level anonymization rules that make up this profile.
    /// </summary>
    /// <remarks>
    /// Each rule specifies a field name, technique, and optional parameters.
    /// Fields not covered by any rule are left untransformed.
    /// </remarks>
    public required IReadOnlyList<FieldAnonymizationRule> FieldRules { get; init; }

    /// <summary>
    /// Timestamp when this profile was created (UTC).
    /// </summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>
    /// Creates a new anonymization profile with a generated unique identifier.
    /// </summary>
    /// <param name="name">Human-readable name for the profile.</param>
    /// <param name="fieldRules">The field-level anonymization rules.</param>
    /// <param name="description">Optional description of the profile's purpose.</param>
    /// <returns>A new <see cref="AnonymizationProfile"/> with a generated GUID identifier and the current UTC timestamp.</returns>
    public static AnonymizationProfile Create(
        string name,
        IReadOnlyList<FieldAnonymizationRule> fieldRules,
        string? description = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Description = description,
            FieldRules = fieldRules,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };
}
