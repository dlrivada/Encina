namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Classification of entities under the NIS2 Directive (EU 2022/2555).
/// </summary>
/// <remarks>
/// <para>
/// Per NIS2 Article 3, entities in scope are classified as either "essential" or "important"
/// based on their size and the sector they operate in. This classification determines:
/// </para>
/// <list type="bullet">
/// <item><description>The level of supervisory oversight (ex-ante for essential, ex-post for important).</description></item>
/// <item><description>The maximum administrative fines (Art. 34): EUR 10M / 2% turnover for essential, EUR 7M / 1.4% for important.</description></item>
/// <item><description>The incident reporting obligations and timelines (Art. 23).</description></item>
/// </list>
/// </remarks>
public enum NIS2EntityType
{
    /// <summary>
    /// Essential entity — subject to ex-ante supervisory regime.
    /// </summary>
    /// <remarks>
    /// Per Art. 3(1), essential entities include large enterprises in Annex I sectors
    /// (energy, transport, banking, health, digital infrastructure, etc.) and certain
    /// entities regardless of size (e.g., trust service providers, DNS service providers,
    /// TLD name registries, public electronic communications networks).
    /// Maximum fines: EUR 10,000,000 or 2% of total worldwide annual turnover (Art. 34(4)).
    /// </remarks>
    Essential = 0,

    /// <summary>
    /// Important entity — subject to ex-post supervisory regime.
    /// </summary>
    /// <remarks>
    /// Per Art. 3(2), important entities include medium-sized enterprises in Annex I sectors
    /// and medium or large enterprises in Annex II sectors (postal services, waste management,
    /// chemical manufacturing, food production, manufacturing, digital providers, research).
    /// Maximum fines: EUR 7,000,000 or 1.4% of total worldwide annual turnover (Art. 34(5)).
    /// </remarks>
    Important = 1
}
