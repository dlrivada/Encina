using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.CrossBorderTransfer.Model;

/// <summary>
/// Identifies the legal basis under which an international data transfer has been approved.
/// </summary>
/// <remarks>
/// <para>
/// This enum represents the <em>operational outcome</em> of transfer validation, indicating
/// which GDPR Chapter V mechanism was used to authorize a specific transfer. It is distinct
/// from <see cref="TransferLegalBasis"/>, which
/// enumerates all possible legal bases defined in the regulation.
/// </para>
/// <para>
/// <see cref="TransferBasis"/> is used in <see cref="TransferValidationOutcome"/> and
/// approved transfer records to document the specific basis that justified the transfer,
/// enabling audit trail and compliance reporting.
/// </para>
/// </remarks>
public enum TransferBasis
{
    /// <summary>
    /// Transfer authorized based on an adequacy decision by the European Commission (Art. 45).
    /// </summary>
    /// <remarks>
    /// The destination country has been assessed by the Commission as providing an adequate
    /// level of data protection. No additional safeguards or Transfer Impact Assessment
    /// are required. Examples include Japan, South Korea, UK, and the EU-US Data Privacy Framework.
    /// </remarks>
    AdequacyDecision = 0,

    /// <summary>
    /// Transfer authorized based on executed Standard Contractual Clauses (Art. 46(2)(c)).
    /// </summary>
    /// <remarks>
    /// Requires a valid, executed SCC agreement with the appropriate module
    /// (<see cref="SCCModule"/>). Post-Schrems II, a Transfer Impact Assessment must
    /// confirm that the destination country's legal framework does not impair the
    /// effectiveness of the SCCs, and supplementary measures may be required.
    /// </remarks>
    SCCs = 1,

    /// <summary>
    /// Transfer authorized based on approved Binding Corporate Rules (Art. 47).
    /// </summary>
    /// <remarks>
    /// BCRs are internal rules adopted by a multinational group of companies for intra-group
    /// transfers. They require approval from a lead supervisory authority and provide
    /// enforceable rights for data subjects across the corporate group.
    /// </remarks>
    BindingCorporateRules = 2,

    /// <summary>
    /// Transfer authorized based on a derogation under Article 49.
    /// </summary>
    /// <remarks>
    /// Derogations are exceptions that allow transfers in specific circumstances, such as
    /// explicit consent (Art. 49(1)(a)), contract performance (Art. 49(1)(b)), public interest
    /// (Art. 49(1)(d)), legal claims (Art. 49(1)(e)), or vital interests (Art. 49(1)(f)).
    /// Derogations should be interpreted restrictively and are not suitable for systematic
    /// or large-scale transfers.
    /// </remarks>
    Derogation = 3,

    /// <summary>
    /// Transfer is blocked — no valid legal basis could be established.
    /// </summary>
    /// <remarks>
    /// Indicates that the transfer validation determined no adequate protection mechanism
    /// is in place for the destination country. The transfer must not proceed until a valid
    /// basis is established (e.g., SCC execution, adequacy decision, or approved derogation).
    /// </remarks>
    Blocked = 4
}
