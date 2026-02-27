namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request to restrict processing of personal data under GDPR Article 18.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to obtain from the controller restriction of processing
/// where one of the following applies: the accuracy of the data is contested, processing
/// is unlawful, the controller no longer needs the data, or the data subject has objected
/// to processing pending verification.
/// </para>
/// <para>
/// While processing is restricted, the data may only be stored — not processed — except
/// with the data subject's consent, for legal claims, for the protection of the rights
/// of another person, or for reasons of important public interest (Article 18(2)).
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject requesting restriction.</param>
/// <param name="Reason">The reason for requesting restriction of processing.</param>
/// <param name="Scope">
/// Optional categories to restrict. <c>null</c> to restrict all processing for the subject.
/// </param>
public sealed record RestrictionRequest(
    string SubjectId,
    string Reason,
    IReadOnlyList<PersonalDataCategory>? Scope);
