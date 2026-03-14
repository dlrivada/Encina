namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Describes a field in a request type that has been analyzed for Privacy by Design compliance.
/// </summary>
/// <remarks>
/// <para>
/// Represents metadata about a single property in a request type, including its declared purpose
/// and whether it is required for the processing operation. Used in <see cref="MinimizationReport"/>
/// to provide actionable field-level analysis.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "only personal data which are necessary for each specific purpose
/// of the processing are processed." This record captures the necessity assessment for each field.
/// </para>
/// </remarks>
/// <param name="FieldName">The name of the property on the request type.</param>
/// <param name="Purpose">
/// The declared purpose for this field via <c>[PurposeLimitation]</c>, or <see langword="null"/>
/// if no purpose is declared.
/// </param>
/// <param name="IsRequired">
/// Whether this field is considered necessary for the processing operation.
/// Fields without <c>[NotStrictlyNecessary]</c> are considered required.
/// </param>
public sealed record PrivacyFieldInfo(
    string FieldName,
    string? Purpose,
    bool IsRequired);
