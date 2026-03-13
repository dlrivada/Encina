namespace Encina.Compliance.ProcessorAgreements.Model;

/// <summary>
/// Tracks compliance with the eight mandatory contractual terms required by GDPR Article 28(3).
/// </summary>
/// <remarks>
/// <para>
/// Article 28(3) stipulates that the contract between controller and processor "shall set out"
/// specific provisions. Each boolean property maps to one of the eight sub-paragraphs (a)-(h).
/// </para>
/// <para>
/// Use <see cref="IsFullyCompliant"/> to quickly verify all mandatory terms are present,
/// or <see cref="MissingTerms"/> to identify specific gaps for remediation.
/// </para>
/// </remarks>
public sealed record DPAMandatoryTerms
{
    /// <summary>
    /// Whether the agreement requires the processor to process personal data only on
    /// documented instructions from the controller.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(a): "processes the personal data only on documented instructions from the
    /// controller, including with regard to transfers of personal data to a third country."
    /// </remarks>
    public required bool ProcessOnDocumentedInstructions { get; init; }

    /// <summary>
    /// Whether the agreement ensures that persons authorised to process personal data
    /// have committed themselves to confidentiality.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(b): "ensures that persons authorised to process the personal data have
    /// committed themselves to confidentiality or are under an appropriate statutory obligation
    /// of confidentiality."
    /// </remarks>
    public required bool ConfidentialityObligations { get; init; }

    /// <summary>
    /// Whether the agreement requires the processor to implement appropriate technical
    /// and organisational security measures.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(c): "takes all measures required pursuant to Article 32" (security of processing).
    /// </remarks>
    public required bool SecurityMeasures { get; init; }

    /// <summary>
    /// Whether the agreement specifies conditions for engaging sub-processors.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(d): "respects the conditions referred to in paragraphs 2 and 4 for
    /// engaging another processor" (sub-processor requirements).
    /// </remarks>
    public required bool SubProcessorRequirements { get; init; }

    /// <summary>
    /// Whether the agreement requires the processor to assist the controller in responding
    /// to data subject rights requests.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(e): "taking into account the nature of the processing, assists the
    /// controller [...] for the fulfilment of the controller's obligation to respond to
    /// requests for exercising the data subject's rights laid down in Chapter III."
    /// </remarks>
    public required bool DataSubjectRightsAssistance { get; init; }

    /// <summary>
    /// Whether the agreement requires the processor to assist the controller in ensuring
    /// compliance with obligations under Articles 32-36.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(f): "assists the controller in ensuring compliance with the obligations
    /// pursuant to Articles 32 to 36" (security, breach notification, DPIA, prior consultation).
    /// </remarks>
    public required bool ComplianceAssistance { get; init; }

    /// <summary>
    /// Whether the agreement requires the processor to delete or return all personal data
    /// after the end of the provision of services.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(g): "at the choice of the controller, deletes or returns all the personal
    /// data to the controller after the end of the provision of services relating to processing."
    /// </remarks>
    public required bool DataDeletionOrReturn { get; init; }

    /// <summary>
    /// Whether the agreement grants the controller audit rights over the processor.
    /// </summary>
    /// <remarks>
    /// Article 28(3)(h): "makes available to the controller all information necessary to
    /// demonstrate compliance with the obligations laid down in this Article and allow for
    /// and contribute to audits, including inspections, conducted by the controller."
    /// </remarks>
    public required bool AuditRights { get; init; }

    /// <summary>
    /// Gets a value indicating whether all eight mandatory terms are present in the agreement.
    /// </summary>
    /// <remarks>
    /// A fully compliant agreement has all eight terms from Article 28(3)(a)-(h) set to
    /// <see langword="true"/>. Use <see cref="MissingTerms"/> to identify specific gaps.
    /// </remarks>
    public bool IsFullyCompliant =>
        ProcessOnDocumentedInstructions &&
        ConfidentialityObligations &&
        SecurityMeasures &&
        SubProcessorRequirements &&
        DataSubjectRightsAssistance &&
        ComplianceAssistance &&
        DataDeletionOrReturn &&
        AuditRights;

    /// <summary>
    /// Gets the list of mandatory term names that are not present in the agreement.
    /// </summary>
    /// <remarks>
    /// Returns an empty list when <see cref="IsFullyCompliant"/> is <see langword="true"/>.
    /// Each entry corresponds to the property name of the missing term (e.g.,
    /// <c>"ProcessOnDocumentedInstructions"</c> for Article 28(3)(a)).
    /// </remarks>
    public IReadOnlyList<string> MissingTerms
    {
        get
        {
            if (IsFullyCompliant)
                return [];

            var missing = new List<string>(8);

            if (!ProcessOnDocumentedInstructions) missing.Add(nameof(ProcessOnDocumentedInstructions));
            if (!ConfidentialityObligations) missing.Add(nameof(ConfidentialityObligations));
            if (!SecurityMeasures) missing.Add(nameof(SecurityMeasures));
            if (!SubProcessorRequirements) missing.Add(nameof(SubProcessorRequirements));
            if (!DataSubjectRightsAssistance) missing.Add(nameof(DataSubjectRightsAssistance));
            if (!ComplianceAssistance) missing.Add(nameof(ComplianceAssistance));
            if (!DataDeletionOrReturn) missing.Add(nameof(DataDeletionOrReturn));
            if (!AuditRights) missing.Add(nameof(AuditRights));

            return missing;
        }
    }
}
