namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Response to a data portability request under GDPR Article 20.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to receive their personal data in a structured,
/// commonly used, and machine-readable format, and to transmit that data to another
/// controller without hindrance (Article 20(1)).
/// </para>
/// <para>
/// This response wraps the <see cref="ExportedData"/> with subject identification
/// and a generation timestamp for audit purposes.
/// </para>
/// </remarks>
public sealed record PortabilityResponse
{
    /// <summary>
    /// Identifier of the data subject whose data was exported.
    /// </summary>
    public required string SubjectId { get; init; }

    /// <summary>
    /// The exported personal data in the requested format.
    /// </summary>
    public required ExportedData ExportedData { get; init; }

    /// <summary>
    /// Timestamp when this portability response was generated (UTC).
    /// </summary>
    public required DateTimeOffset GeneratedAtUtc { get; init; }
}
