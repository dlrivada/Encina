namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Legal basis for an international data transfer under GDPR Chapter V.
/// </summary>
/// <remarks>
/// <para>
/// When personal data is transferred outside the EU/EEA, a valid legal basis
/// must be established. The hierarchy of preference is: adequacy decision (Art. 45),
/// appropriate safeguards (Art. 46), then derogations (Art. 49) as a last resort.
/// </para>
/// <para>
/// The Schrems II judgment (CJEU C-311/18) emphasized that the level of protection
/// must be "essentially equivalent" to EU standards, and that supplementary measures
/// may be needed even when using SCCs or BCRs.
/// </para>
/// </remarks>
public enum TransferLegalBasis
{
    /// <summary>
    /// Transfer based on an adequacy decision by the European Commission (Art. 45).
    /// </summary>
    /// <remarks>
    /// The Commission has determined that the third country ensures an adequate level
    /// of protection. No additional authorization is needed. Current adequacy decisions
    /// cover 15 countries/territories including Japan, South Korea, UK, and the
    /// EU-US Data Privacy Framework.
    /// </remarks>
    AdequacyDecision = 0,

    /// <summary>
    /// Transfer based on Standard Contractual Clauses approved by the Commission (Art. 46(2)(c)).
    /// </summary>
    /// <remarks>
    /// Pre-approved contractual terms that bind the data importer to EU-equivalent
    /// data protection standards. Post-Schrems II, a transfer impact assessment
    /// and supplementary measures may be required.
    /// </remarks>
    StandardContractualClauses = 1,

    /// <summary>
    /// Transfer based on Binding Corporate Rules approved by a supervisory authority (Art. 47).
    /// </summary>
    /// <remarks>
    /// Internal rules adopted by a multinational group of companies for intra-group
    /// transfers. Requires approval from a lead supervisory authority and provides
    /// enforceable rights for data subjects.
    /// </remarks>
    BindingCorporateRules = 2,

    /// <summary>
    /// Transfer based on explicit consent of the data subject (Art. 49(1)(a)).
    /// </summary>
    /// <remarks>
    /// The data subject must be informed of the possible risks of the transfer
    /// due to the absence of an adequacy decision and appropriate safeguards.
    /// Consent must be explicit, specific, and freely given. Should not be used
    /// for repetitive or structural transfers.
    /// </remarks>
    ExplicitConsent = 3,

    /// <summary>
    /// Transfer necessary for important reasons of public interest (Art. 49(1)(d)).
    /// </summary>
    /// <remarks>
    /// The public interest must be recognized in Union or Member State law.
    /// This derogation cannot be relied upon for all transfers — only for
    /// specific public interest objectives.
    /// </remarks>
    PublicInterest = 4,

    /// <summary>
    /// Transfer necessary for the establishment, exercise, or defence of legal claims (Art. 49(1)(e)).
    /// </summary>
    LegalClaims = 5,

    /// <summary>
    /// Transfer necessary to protect vital interests of the data subject (Art. 49(1)(f)).
    /// </summary>
    /// <remarks>
    /// Applies when the data subject is physically or legally incapable of giving consent
    /// and the transfer is necessary for their vital interests or those of another person.
    /// </remarks>
    VitalInterests = 6,

    /// <summary>
    /// Transfer based on another derogation under Article 49.
    /// </summary>
    /// <remarks>
    /// Covers Article 49(1)(b) (performance of contract), 49(1)(c) (contract in interest
    /// of data subject), and 49(2) (limited transfers from a public register).
    /// Derogations should be interpreted restrictively and not used for systematic transfers.
    /// </remarks>
    Derogation = 7
}
