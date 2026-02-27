namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Marks a property as containing personal data subject to GDPR Data Subject Rights.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute on entity properties that store personal data. The DSR infrastructure
/// uses this metadata to:
/// </para>
/// <list type="bullet">
/// <item><b>Discovery</b>: Automatically locate personal data fields via reflection</item>
/// <item><b>Erasure (Article 17)</b>: Identify which fields can be erased and which have legal retention</item>
/// <item><b>Portability (Article 20)</b>: Determine which fields should be included in data exports</item>
/// <item><b>Access (Article 15)</b>: Build comprehensive data subject access responses</item>
/// <item><b>Categorization</b>: Classify data by <see cref="PersonalDataCategory"/> for scoped operations</item>
/// </list>
/// <para>
/// Properties marked with <c>LegalRetention = true</c> will be excluded from erasure operations
/// and retained with a documented reason.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class Customer
/// {
///     public Guid Id { get; set; }
///
///     [PersonalData(Category = PersonalDataCategory.Identity)]
///     public string FullName { get; set; } = string.Empty;
///
///     [PersonalData(Category = PersonalDataCategory.Contact)]
///     public string Email { get; set; } = string.Empty;
///
///     [PersonalData(Category = PersonalDataCategory.Financial, LegalRetention = true,
///         RetentionReason = "Tax records must be retained for 7 years per local law")]
///     public string TaxId { get; set; } = string.Empty;
///
///     [PersonalData(Category = PersonalDataCategory.Health, Erasable = true, Portable = false)]
///     public string BloodType { get; set; } = string.Empty;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PersonalDataAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the category of personal data this property contains.
    /// </summary>
    /// <remarks>
    /// Used for scoped erasure and portability operations. When a data subject requests
    /// erasure of specific categories (e.g., only financial data), only properties in
    /// matching categories are affected.
    /// </remarks>
    public PersonalDataCategory Category { get; set; } = PersonalDataCategory.Other;

    /// <summary>
    /// Gets or sets whether this field can be erased under the right to erasure (Article 17).
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set to <c>false</c> for fields that cannot be erased
    /// due to technical constraints. For legal retention requirements, use
    /// <see cref="LegalRetention"/> instead.
    /// </remarks>
    public bool Erasable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this field should be included in data portability exports (Article 20).
    /// </summary>
    /// <remarks>
    /// Defaults to <c>true</c>. Set to <c>false</c> for derived or computed data that
    /// should not be included in portability exports.
    /// </remarks>
    public bool Portable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this field has a legal retention requirement that prevents erasure.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, the field will be retained even during erasure operations, and the
    /// <see cref="RetentionReason"/> should document the legal basis for retention.
    /// Per Article 17(3), erasure may be refused when processing is necessary for compliance
    /// with a legal obligation or for archiving purposes in the public interest.
    /// </remarks>
    public bool LegalRetention { get; set; }

    /// <summary>
    /// Gets or sets the reason why this field must be retained despite erasure requests.
    /// </summary>
    /// <remarks>
    /// Only meaningful when <see cref="LegalRetention"/> is <c>true</c>. Should reference
    /// the specific legal basis (e.g., "Tax records retained per Directive 2006/112/EC").
    /// This reason is included in the <see cref="ErasureResult.RetentionReasons"/> collection.
    /// </remarks>
    public string? RetentionReason { get; set; }
}
