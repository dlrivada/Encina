namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// The result of validating a request's fields against a declared processing purpose.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <c>IPrivacyByDesignValidator.ValidatePurposeLimitationAsync</c>, this record
/// captures which fields are allowed for the declared purpose and which violate purpose limitation.
/// </para>
/// <para>
/// Per GDPR Article 5(1)(b), personal data shall be "collected for specified, explicit and
/// legitimate purposes and not further processed in a manner that is incompatible with those
/// purposes." This validation enforces purpose limitation at the request field level.
/// </para>
/// </remarks>
/// <param name="DeclaredPurpose">The processing purpose that was validated against.</param>
/// <param name="AllowedFields">The fields that are permitted for the declared purpose.</param>
/// <param name="ViolatingFields">
/// The fields that are not permitted for the declared purpose. These fields either have a
/// <c>[PurposeLimitation]</c> attribute declaring a different purpose, or are not in the
/// purpose's allowed fields list.
/// </param>
/// <param name="IsValid">
/// Whether all fields comply with the declared purpose.
/// Equivalent to <c>ViolatingFields.Count == 0</c>.
/// </param>
public sealed record PurposeValidationResult(
    string DeclaredPurpose,
    IReadOnlyList<string> AllowedFields,
    IReadOnlyList<string> ViolatingFields,
    bool IsValid);
