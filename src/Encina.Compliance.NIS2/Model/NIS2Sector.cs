namespace Encina.Compliance.NIS2.Model;

/// <summary>
/// Sectors covered by the NIS2 Directive (EU 2022/2555), as defined in Annexes I and II.
/// </summary>
/// <remarks>
/// <para>
/// NIS2 applies to entities operating in 18 sectors across two annexes:
/// </para>
/// <list type="bullet">
/// <item><description><strong>Annex I</strong> (11 sectors of high criticality): Entities in these sectors
/// are classified as <see cref="NIS2EntityType.Essential"/> when large, or <see cref="NIS2EntityType.Important"/>
/// when medium-sized.</description></item>
/// <item><description><strong>Annex II</strong> (7 other critical sectors): Entities in these sectors
/// are classified as <see cref="NIS2EntityType.Important"/> regardless of size (medium or large).</description></item>
/// </list>
/// <para>
/// Per Art. 2(1), the Directive applies to public or private entities that qualify as medium-sized
/// or larger under the SME Recommendation (2003/361/EC) and provide services or carry out activities
/// within the EU in one of these sectors.
/// </para>
/// </remarks>
public enum NIS2Sector
{
    // --------------------------------------------------
    // Annex I — Sectors of High Criticality
    // --------------------------------------------------

    /// <summary>
    /// Energy sector (electricity, oil, gas, hydrogen, district heating/cooling).
    /// </summary>
    /// <remarks>Annex I, Sector 1. Includes electricity undertakings, distribution/transmission system operators,
    /// producers, nominated electricity market operators (NEMOs), oil pipeline operators, LNG terminal operators,
    /// refineries, and hydrogen producers/storage/transmission operators.</remarks>
    Energy = 0,

    /// <summary>
    /// Transport sector (air, rail, water, road).
    /// </summary>
    /// <remarks>Annex I, Sector 2. Includes air carriers, airport managing bodies, traffic management control operators,
    /// railway undertakings, infrastructure managers, inland waterway/maritime transport companies, port authorities,
    /// vessel traffic services, and intelligent transport systems operators.</remarks>
    Transport = 1,

    /// <summary>
    /// Banking sector.
    /// </summary>
    /// <remarks>Annex I, Sector 3. Includes credit institutions as defined in Regulation (EU) No 575/2013.
    /// Subject to additional DORA (Digital Operational Resilience Act) requirements.</remarks>
    Banking = 2,

    /// <summary>
    /// Financial market infrastructure.
    /// </summary>
    /// <remarks>Annex I, Sector 4. Includes operators of trading venues and central counterparties (CCPs)
    /// as defined in Regulation (EU) No 648/2012.</remarks>
    FinancialMarketInfrastructure = 3,

    /// <summary>
    /// Health sector.
    /// </summary>
    /// <remarks>Annex I, Sector 5. Includes healthcare providers, EU reference laboratories, entities
    /// carrying out R&amp;D of medicinal products, entities manufacturing basic pharmaceutical products and
    /// preparations, and entities manufacturing medical devices considered critical during a public health emergency.</remarks>
    Health = 4,

    /// <summary>
    /// Drinking water supply and distribution.
    /// </summary>
    /// <remarks>Annex I, Sector 6. Includes suppliers and distributors of water intended for human consumption
    /// as defined in Directive (EU) 2020/2184, excluding distributors for whom distribution is a non-essential
    /// part of their general activity.</remarks>
    DrinkingWater = 5,

    /// <summary>
    /// Waste water collection, disposal, or treatment.
    /// </summary>
    /// <remarks>Annex I, Sector 7. Includes undertakings collecting, disposing of, or treating urban waste water,
    /// domestic waste water, or industrial waste water as defined in Directive 91/271/EEC.</remarks>
    WasteWater = 6,

    /// <summary>
    /// Digital infrastructure.
    /// </summary>
    /// <remarks>Annex I, Sector 8. Includes internet exchange point (IXP) providers, DNS service providers,
    /// TLD name registries, cloud computing service providers, data centre service providers, content delivery
    /// network (CDN) providers, trust service providers, and providers of public electronic communications
    /// networks or publicly available electronic communications services.</remarks>
    DigitalInfrastructure = 7,

    /// <summary>
    /// ICT service management (business-to-business).
    /// </summary>
    /// <remarks>Annex I, Sector 9. Includes managed service providers (MSPs) and managed security service
    /// providers (MSSPs). These entities are essential regardless of size due to their systemic importance
    /// in the supply chain.</remarks>
    ICTServiceManagement = 8,

    /// <summary>
    /// Public administration (central and regional level).
    /// </summary>
    /// <remarks>Annex I, Sector 10. Includes public administration entities of central governments and
    /// public administration entities at regional level as defined by Member States in accordance with
    /// national law. Excludes judiciary, parliaments, and central banks.</remarks>
    PublicAdministration = 9,

    /// <summary>
    /// Space sector.
    /// </summary>
    /// <remarks>Annex I, Sector 11. Includes operators of ground-based infrastructure owned, managed,
    /// and operated by Member States or private parties that support the provision of space-based services,
    /// excluding providers of public electronic communications networks.</remarks>
    Space = 10,

    // --------------------------------------------------
    // Annex II — Other Critical Sectors
    // --------------------------------------------------

    /// <summary>
    /// Postal and courier services.
    /// </summary>
    /// <remarks>Annex II, Sector 1. Includes postal service providers as defined in Directive 97/67/EC,
    /// including providers of courier services.</remarks>
    PostalAndCourier = 11,

    /// <summary>
    /// Waste management (excluding waste water).
    /// </summary>
    /// <remarks>Annex II, Sector 2. Includes undertakings carrying out waste management as defined in
    /// Directive 2008/98/EC, excluding undertakings for whom waste management is not their principal
    /// economic activity.</remarks>
    WasteManagement = 12,

    /// <summary>
    /// Manufacture, production, and distribution of chemicals.
    /// </summary>
    /// <remarks>Annex II, Sector 3. Includes undertakings carrying out manufacture of substances and
    /// distribution of substances or mixtures as referred to in Regulation (EC) No 1907/2006 (REACH)
    /// and Regulation (EC) No 1272/2008 (CLP).</remarks>
    ChemicalManufacturing = 13,

    /// <summary>
    /// Production, processing, and distribution of food.
    /// </summary>
    /// <remarks>Annex II, Sector 4. Includes food businesses as defined in Regulation (EC) No 178/2002
    /// engaged in wholesale distribution, industrial production, or processing.</remarks>
    FoodProduction = 14,

    /// <summary>
    /// Manufacturing (medical devices, electronics, machinery, motor vehicles, other transport equipment).
    /// </summary>
    /// <remarks>Annex II, Sector 5. Covers manufacturers of: medical devices and in vitro diagnostic
    /// medical devices (Regulations (EU) 2017/745 and 2017/746); computer, electronic, and optical products
    /// (NACE C26); electrical equipment (NACE C27); machinery and equipment n.e.c. (NACE C28);
    /// motor vehicles, trailers and semi-trailers (NACE C29); other transport equipment (NACE C30).</remarks>
    Manufacturing = 15,

    /// <summary>
    /// Digital providers (online marketplaces, search engines, social networking platforms).
    /// </summary>
    /// <remarks>Annex II, Sector 6. Includes providers of online marketplaces, online search engines,
    /// and social networking services platforms as defined in Regulation (EU) 2022/2065
    /// (Digital Services Act).</remarks>
    DigitalProviders = 16,

    /// <summary>
    /// Research organizations.
    /// </summary>
    /// <remarks>Annex II, Sector 7. Includes research organisations as defined by national law whose
    /// primary goal is to carry out applied research or experimental development with a view to
    /// exploiting the results of that research for commercial purposes. Excludes educational institutions.</remarks>
    Research = 17
}
