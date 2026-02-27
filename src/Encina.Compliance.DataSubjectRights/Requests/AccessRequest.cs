namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request for a data subject to access their personal data under GDPR Article 15.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to obtain from the controller confirmation as to whether
/// personal data concerning them is being processed, and if so, access to the personal data
/// and supplementary information (Article 15(1)).
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject requesting access.</param>
/// <param name="IncludeProcessingActivities">
/// Whether to include associated processing activities in the response.
/// When <c>true</c>, the response includes supplementary information as required by Article 15(1)(a-h).
/// </param>
public sealed record AccessRequest(
    string SubjectId,
    bool IncludeProcessingActivities);
