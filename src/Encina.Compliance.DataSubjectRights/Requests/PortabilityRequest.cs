namespace Encina.Compliance.DataSubjectRights;

/// <summary>
/// Request to export personal data in a portable format under GDPR Article 20.
/// </summary>
/// <remarks>
/// <para>
/// The data subject has the right to receive their personal data in a structured,
/// commonly used, and machine-readable format, and to transmit that data to another
/// controller without hindrance.
/// </para>
/// <para>
/// This right applies only to data processed by automated means and where the processing
/// is based on consent (Article 6(1)(a)) or contract (Article 6(1)(b)).
/// </para>
/// </remarks>
/// <param name="SubjectId">Identifier of the data subject requesting portability.</param>
/// <param name="Format">The desired export format (JSON, CSV, or XML).</param>
/// <param name="Categories">
/// Optional categories to include in the export. <c>null</c> to export all portable data.
/// </param>
public sealed record PortabilityRequest(
    string SubjectId,
    ExportFormat Format,
    IReadOnlyList<PersonalDataCategory>? Categories);
