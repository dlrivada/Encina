namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request to erase personal data under GDPR Article 17 ("right to be forgotten").
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to obtain from the controller the erasure of personal
/// data concerning them without undue delay, provided that one of the grounds in
/// Article 17(1) applies and no exemptions under Article 17(3) prevent the erasure.
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject requesting erasure.</param>
/// <param name="Reason">The legal ground for erasure as stated by the data subject (Article 17(1)(a-f)).</param>
/// <param name="Scope">
/// Optional scope to narrow the erasure to specific categories or fields.
/// <c>null</c> to erase all erasable personal data for the subject.
/// </param>
public sealed record ErasureRequest(
    string SubjectId,
    ErasureReason Reason,
    ErasureScope? Scope);
