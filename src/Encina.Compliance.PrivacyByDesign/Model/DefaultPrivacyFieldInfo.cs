namespace Encina.Compliance.PrivacyByDesign.Model;

/// <summary>
/// Describes a field that has a declared privacy-respecting default value and its current state.
/// </summary>
/// <remarks>
/// <para>
/// Used by the default privacy validation to track fields decorated with <c>[PrivacyDefault]</c>.
/// The validator checks whether the field's actual value matches the declared default; deviations
/// indicate that the user or system has explicitly opted into more permissive data processing.
/// </para>
/// <para>
/// Per GDPR Article 25(2), "the controller shall implement appropriate technical and
/// organisational measures for ensuring that, by default, personal data are not made
/// accessible without the individual's intervention to an indefinite number of natural persons."
/// </para>
/// </remarks>
/// <param name="FieldName">The name of the property on the request type.</param>
/// <param name="DeclaredDefault">
/// The declared privacy-respecting default value from <c>[PrivacyDefault]</c>.
/// May be <see langword="null"/> when <see langword="null"/> is the privacy-respecting default.
/// </param>
/// <param name="ActualValue">
/// The actual value of the field in the current request instance.
/// May be <see langword="null"/> if the field is not set.
/// </param>
/// <param name="MatchesDefault">
/// Whether the actual value matches the declared privacy-respecting default.
/// When <see langword="false"/>, the field deviates from the privacy default.
/// </param>
public sealed record DefaultPrivacyFieldInfo(
    string FieldName,
    object? DeclaredDefault,
    object? ActualValue,
    bool MatchesDefault);
