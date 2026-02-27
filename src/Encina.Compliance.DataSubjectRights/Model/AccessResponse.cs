using Encina.Compliance.GDPR;

namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Response to a data subject access request under GDPR Article 15.
/// </summary>
/// <remarks>
/// <para>
/// The controller must provide the data subject with a copy of the personal data undergoing
/// processing (Article 15(3)), along with supplementary information about the processing
/// including purposes, categories, recipients, retention periods, and rights.
/// </para>
/// <para>
/// This response aggregates all located personal data and, optionally, the associated
/// processing activities that document how and why the data is processed.
/// </para>
/// </remarks>
public sealed record AccessResponse
{
    /// <summary>
    /// Identifier of the data subject whose data is being returned.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// All personal data locations found for this data subject.
    /// </summary>
    /// <remarks>
    /// Each entry identifies a specific field in a specific entity instance,
    /// including the current value of the data.
    /// </remarks>
    public required IReadOnlyList<PersonalDataLocation> Data { get; init; }

    /// <summary>
    /// Processing activities associated with this data subject's data.
    /// </summary>
    /// <remarks>
    /// Included when the access request specifies <c>IncludeProcessingActivities = true</c>.
    /// Provides the supplementary information required by Article 15(1)(a-h).
    /// </remarks>
    public required IReadOnlyList<ProcessingActivity> ProcessingActivities { get; init; }

    /// <summary>
    /// Timestamp when this access response was generated (UTC).
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }
}
