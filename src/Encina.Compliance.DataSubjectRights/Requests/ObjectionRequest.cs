namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request to object to processing of personal data under GDPR Article 21.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to object, on grounds relating to their particular
/// situation, to processing based on Article 6(1)(e) (public interest) or Article 6(1)(f)
/// (legitimate interests). The controller must cease processing unless compelling
/// legitimate grounds override the data subject's interests, rights, and freedoms.
/// </para>
/// <para>
/// For direct marketing purposes, the right to object is absolute — the controller
/// must cease processing upon objection without any balancing test (Article 21(2-3)).
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject raising the objection.</param>
/// <param name="ProcessingPurpose">The specific processing purpose being objected to.</param>
/// <param name="Reason">
/// Grounds relating to the data subject's particular situation that justify the objection.
/// </param>
public sealed record ObjectionRequest(
    string SubjectId,
    string ProcessingPurpose,
    string Reason);
