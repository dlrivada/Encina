namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Tracks management body accountability for cybersecurity under NIS2 Article 20.
/// </summary>
/// <remarks>
/// <para>
/// NIS2 introduces a significant change from NIS1 by establishing <strong>personal liability</strong>
/// for management bodies. Per Art. 20(1), Member States must ensure that the management bodies of
/// essential and important entities approve the cybersecurity risk-management measures taken by
/// those entities and oversee their implementation, and that management body members can be held
/// liable for infringements.
/// </para>
/// <para>
/// Per Art. 20(2), Member States must ensure that members of management bodies are required to
/// follow training and encourage entities to offer similar training to their employees on a
/// regular basis, in order to gain sufficient knowledge and skills to enable them to identify
/// risks and assess cybersecurity risk-management practices and their impact on the services
/// provided by the entity.
/// </para>
/// <para>
/// Per Art. 32(6) and Art. 33(5), competent authorities may request that management bodies
/// be temporarily suspended from exercising managerial functions in the event of non-compliance.
/// </para>
/// </remarks>
public sealed record ManagementAccountabilityRecord
{
    /// <summary>
    /// Name of the responsible person in the management body.
    /// </summary>
    public required string ResponsiblePerson { get; init; }

    /// <summary>
    /// Role or title within the management body.
    /// </summary>
    /// <remarks>
    /// Examples: "Chief Information Security Officer", "Chief Technology Officer",
    /// "Board Member for Digital Affairs", "Managing Director".
    /// </remarks>
    public required string Role { get; init; }

    /// <summary>
    /// Timestamp when the responsible person acknowledged their NIS2 accountability (UTC).
    /// </summary>
    /// <remarks>
    /// Per Art. 20(1), management body members must approve and oversee cybersecurity measures.
    /// This timestamp records formal acknowledgement of that responsibility.
    /// </remarks>
    public required DateTimeOffset AcknowledgedAtUtc { get; init; }

    /// <summary>
    /// Areas of NIS2 compliance overseen by this management body member.
    /// </summary>
    /// <remarks>
    /// Maps to one or more <see cref="NIS2Measure"/> areas that this person is responsible
    /// for overseeing. For example: risk analysis, incident handling, supply chain security.
    /// </remarks>
    public required IReadOnlyList<string> ComplianceAreas { get; init; }

    /// <summary>
    /// Timestamp when the management body member last completed cybersecurity training (UTC).
    /// </summary>
    /// <remarks>
    /// Per Art. 20(2), management body members must follow training to gain sufficient
    /// knowledge and skills. A <c>null</c> value indicates training has not been completed,
    /// which is a compliance gap.
    /// </remarks>
    public DateTimeOffset? TrainingCompletedAtUtc { get; init; }

    /// <summary>
    /// Creates a new management accountability record.
    /// </summary>
    /// <param name="responsiblePerson">Name of the responsible person.</param>
    /// <param name="role">Role or title within the management body.</param>
    /// <param name="acknowledgedAtUtc">Timestamp of accountability acknowledgement.</param>
    /// <param name="complianceAreas">Areas of compliance oversight.</param>
    /// <returns>A new <see cref="ManagementAccountabilityRecord"/>.</returns>
    public static ManagementAccountabilityRecord Create(
        string responsiblePerson,
        string role,
        DateTimeOffset acknowledgedAtUtc,
        IReadOnlyList<string> complianceAreas) =>
        new()
        {
            ResponsiblePerson = responsiblePerson,
            Role = role,
            AcknowledgedAtUtc = acknowledgedAtUtc,
            ComplianceAreas = complianceAreas
        };
}
