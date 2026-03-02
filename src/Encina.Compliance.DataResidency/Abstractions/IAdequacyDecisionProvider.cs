using Encina.Compliance.DataResidency.Model;

namespace Encina.Compliance.DataResidency;

/// <summary>
/// Provides information about EU adequacy decisions for data transfer compliance.
/// </summary>
/// <remarks>
/// <para>
/// Per GDPR Article 45, the European Commission may decide that a third country,
/// a territory or one or more specified sectors within a third country, or an international
/// organisation ensures an adequate level of data protection. Transfers to such countries
/// do not require specific authorisation.
/// </para>
/// <para>
/// The adequacy decision provider encapsulates the current list of countries and territories
/// with an EU adequacy decision, allowing the <see cref="ICrossBorderTransferValidator"/>
/// to determine whether a destination region qualifies for simplified transfer approval.
/// </para>
/// <para>
/// Unlike other interfaces in this module, adequacy decision methods are synchronous
/// because adequacy status is a well-known, relatively static dataset that can be resolved
/// without I/O. Implementations typically use the <see cref="RegionRegistry"/> or a
/// cached configuration source.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if Japan has an EU adequacy decision
/// bool hasAdequacy = adequacyProvider.HasAdequacy(RegionRegistry.Japan);
/// // Returns true — Japan received adequacy on January 23, 2019
///
/// // Get all regions with EU adequacy decisions
/// var adequateRegions = adequacyProvider.GetAdequateRegions();
/// </code>
/// </example>
public interface IAdequacyDecisionProvider
{
    /// <summary>
    /// Determines whether the specified region has an EU adequacy decision.
    /// </summary>
    /// <param name="region">The region to check for adequacy status.</param>
    /// <returns>
    /// <c>true</c> if the region has an EU adequacy decision (per Art. 45) or is within
    /// the EEA (where GDPR applies directly), <c>false</c> otherwise.
    /// </returns>
    /// <remarks>
    /// EEA member states (EU + Iceland, Liechtenstein, Norway) are implicitly considered
    /// adequate because GDPR applies directly within the EEA. Transfers between EEA
    /// countries do not constitute "international transfers" under GDPR Chapter V.
    /// </remarks>
    bool HasAdequacy(Region region);

    /// <summary>
    /// Retrieves all regions that have an EU adequacy decision or are within the EEA.
    /// </summary>
    /// <returns>
    /// A read-only list of all <see cref="Region"/> instances that are considered adequate
    /// for data transfers, including both EEA member states and third countries with
    /// formal adequacy decisions.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The list includes:
    /// - All EU member states (27 countries)
    /// - EEA-only countries (Iceland, Liechtenstein, Norway)
    /// - Third countries with EU adequacy decisions (e.g., Japan, South Korea, UK,
    ///   Switzerland, Canada, New Zealand, United States under EU-US Data Privacy Framework)
    /// </para>
    /// <para>
    /// Used by the <see cref="ICrossBorderTransferValidator"/> to determine whether a
    /// transfer qualifies for the simplified adequacy-based approval path.
    /// </para>
    /// </remarks>
    IReadOnlyList<Region> GetAdequateRegions();
}
