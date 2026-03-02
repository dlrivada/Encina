namespace Encina.Compliance.DataResidency.Model;

/// <summary>
/// Provides well-known <see cref="Region"/> instances for data residency enforcement.
/// </summary>
/// <remarks>
/// <para>
/// This registry contains pre-configured regions for all EU/EEA member states, countries
/// with EU adequacy decisions, and major non-adequate countries. Each region includes
/// metadata about its EU/EEA membership, adequacy status, and data protection level.
/// </para>
/// <para>
/// EU adequacy decisions are current as of 2025, based on the European Commission's
/// decisions under GDPR Article 45. The list includes: Andorra, Argentina,
/// Canada (commercial), Faroe Islands, Guernsey, Israel, Isle of Man, Japan, Jersey,
/// New Zealand, Republic of Korea, Switzerland, United Kingdom, Uruguay, and the
/// United States (EU-US Data Privacy Framework).
/// </para>
/// <para>
/// For custom regions (e.g., private cloud zones, internal data centers), use
/// <see cref="Region.Create"/> to create new instances with appropriate metadata.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Access well-known regions
/// var germany = RegionRegistry.DE;
/// var japan = RegionRegistry.JP;
///
/// // Look up a region by code
/// var region = RegionRegistry.GetByCode("FR");
///
/// // Iterate EU member states
/// foreach (var member in RegionRegistry.EUMemberStates)
/// {
///     Console.WriteLine($"{member.Code}: EU={member.IsEU}, Adequacy={member.HasAdequacyDecision}");
/// }
/// </code>
/// </example>
public static class RegionRegistry
{
    // ---------------------------------------------------------------
    //  EU Member States (27) — GDPR applies directly, free data flow
    // ---------------------------------------------------------------

    /// <summary>Austria (AT) — EU member state.</summary>
    public static Region AT { get; } = Region.Create("AT", "AT", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Belgium (BE) — EU member state.</summary>
    public static Region BE { get; } = Region.Create("BE", "BE", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Bulgaria (BG) — EU member state.</summary>
    public static Region BG { get; } = Region.Create("BG", "BG", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Croatia (HR) — EU member state.</summary>
    public static Region HR { get; } = Region.Create("HR", "HR", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Cyprus (CY) — EU member state.</summary>
    public static Region CY { get; } = Region.Create("CY", "CY", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Czech Republic (CZ) — EU member state.</summary>
    public static Region CZ { get; } = Region.Create("CZ", "CZ", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Denmark (DK) — EU member state.</summary>
    public static Region DK { get; } = Region.Create("DK", "DK", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Estonia (EE) — EU member state.</summary>
    public static Region EE { get; } = Region.Create("EE", "EE", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Finland (FI) — EU member state.</summary>
    public static Region FI { get; } = Region.Create("FI", "FI", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>France (FR) — EU member state.</summary>
    public static Region FR { get; } = Region.Create("FR", "FR", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Germany (DE) — EU member state.</summary>
    public static Region DE { get; } = Region.Create("DE", "DE", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Greece (GR) — EU member state.</summary>
    public static Region GR { get; } = Region.Create("GR", "GR", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Hungary (HU) — EU member state.</summary>
    public static Region HU { get; } = Region.Create("HU", "HU", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Ireland (IE) — EU member state.</summary>
    public static Region IE { get; } = Region.Create("IE", "IE", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Italy (IT) — EU member state.</summary>
    public static Region IT { get; } = Region.Create("IT", "IT", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Latvia (LV) — EU member state.</summary>
    public static Region LV { get; } = Region.Create("LV", "LV", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Lithuania (LT) — EU member state.</summary>
    public static Region LT { get; } = Region.Create("LT", "LT", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Luxembourg (LU) — EU member state.</summary>
    public static Region LU { get; } = Region.Create("LU", "LU", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Malta (MT) — EU member state.</summary>
    public static Region MT { get; } = Region.Create("MT", "MT", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Netherlands (NL) — EU member state.</summary>
    public static Region NL { get; } = Region.Create("NL", "NL", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Poland (PL) — EU member state.</summary>
    public static Region PL { get; } = Region.Create("PL", "PL", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Portugal (PT) — EU member state.</summary>
    public static Region PT { get; } = Region.Create("PT", "PT", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Romania (RO) — EU member state.</summary>
    public static Region RO { get; } = Region.Create("RO", "RO", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Slovakia (SK) — EU member state.</summary>
    public static Region SK { get; } = Region.Create("SK", "SK", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Slovenia (SI) — EU member state.</summary>
    public static Region SI { get; } = Region.Create("SI", "SI", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Spain (ES) — EU member state.</summary>
    public static Region ES { get; } = Region.Create("ES", "ES", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Sweden (SE) — EU member state.</summary>
    public static Region SE { get; } = Region.Create("SE", "SE", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    // ---------------------------------------------------------------
    //  EEA-only countries (not EU members) — GDPR applies via EEA Agreement
    // ---------------------------------------------------------------

    /// <summary>Iceland (IS) — EEA member (not EU). GDPR applies via EEA Agreement.</summary>
    public static Region IS { get; } = Region.Create("IS", "IS", isEU: false, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Liechtenstein (LI) — EEA member (not EU). GDPR applies via EEA Agreement.</summary>
    public static Region LI { get; } = Region.Create("LI", "LI", isEU: false, isEEA: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Norway (NO) — EEA member (not EU). GDPR applies via EEA Agreement.</summary>
    public static Region NO { get; } = Region.Create("NO", "NO", isEU: false, isEEA: true, protectionLevel: DataProtectionLevel.High);

    // ---------------------------------------------------------------
    //  Countries with EU Adequacy Decisions (Art. 45) — non-EEA
    // ---------------------------------------------------------------

    /// <summary>Andorra (AD) — EU adequacy decision.</summary>
    public static Region AD { get; } = Region.Create("AD", "AD", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Argentina (AR) — EU adequacy decision.</summary>
    public static Region AR { get; } = Region.Create("AR", "AR", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Canada (CA) — EU adequacy decision (commercial organizations under PIPEDA).</summary>
    public static Region CA { get; } = Region.Create("CA", "CA", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Faroe Islands (FO) — EU adequacy decision.</summary>
    public static Region FO { get; } = Region.Create("FO", "FO", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Guernsey (GG) — EU adequacy decision.</summary>
    public static Region GG { get; } = Region.Create("GG", "GG", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Israel (IL) — EU adequacy decision.</summary>
    public static Region IL { get; } = Region.Create("IL", "IL", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Isle of Man (IM) — EU adequacy decision.</summary>
    public static Region IM { get; } = Region.Create("IM", "IM", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Japan (JP) — EU adequacy decision (under APPI).</summary>
    public static Region JP { get; } = Region.Create("JP", "JP", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Jersey (JE) — EU adequacy decision.</summary>
    public static Region JE { get; } = Region.Create("JE", "JE", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>New Zealand (NZ) — EU adequacy decision.</summary>
    public static Region NZ { get; } = Region.Create("NZ", "NZ", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Republic of Korea (KR) — EU adequacy decision (under PIPA).</summary>
    public static Region KR { get; } = Region.Create("KR", "KR", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Switzerland (CH) — EU adequacy decision.</summary>
    public static Region CH { get; } = Region.Create("CH", "CH", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>United Kingdom (GB) — EU adequacy decision (post-Brexit, under UK GDPR).</summary>
    public static Region GB { get; } = Region.Create("GB", "GB", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>Uruguay (UY) — EU adequacy decision.</summary>
    public static Region UY { get; } = Region.Create("UY", "UY", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.High);

    /// <summary>United States (US) — EU adequacy decision (under EU-US Data Privacy Framework).</summary>
    public static Region US { get; } = Region.Create("US", "US", hasAdequacyDecision: true, protectionLevel: DataProtectionLevel.Medium);

    // ---------------------------------------------------------------
    //  Major non-adequate countries — require SCCs, BCRs, or derogation
    // ---------------------------------------------------------------

    /// <summary>Australia (AU) — No EU adequacy decision. Transfers require appropriate safeguards.</summary>
    public static Region AU { get; } = Region.Create("AU", "AU", protectionLevel: DataProtectionLevel.Medium);

    /// <summary>Brazil (BR) — No EU adequacy decision. LGPD provides medium protection. Transfers require appropriate safeguards.</summary>
    public static Region BR { get; } = Region.Create("BR", "BR", protectionLevel: DataProtectionLevel.Medium);

    /// <summary>China (CN) — No EU adequacy decision. PIPL applies. Transfers require appropriate safeguards.</summary>
    public static Region CN { get; } = Region.Create("CN", "CN", protectionLevel: DataProtectionLevel.Low);

    /// <summary>India (IN) — No EU adequacy decision. DPDP Act 2023. Transfers require appropriate safeguards.</summary>
    public static Region IN { get; } = Region.Create("IN", "IN", protectionLevel: DataProtectionLevel.Medium);

    /// <summary>Singapore (SG) — No EU adequacy decision. PDPA applies. Transfers require appropriate safeguards.</summary>
    public static Region SG { get; } = Region.Create("SG", "SG", protectionLevel: DataProtectionLevel.Medium);

    /// <summary>
    /// Composite region representing the European Union as a whole.
    /// </summary>
    /// <remarks>
    /// This is a synthetic region for policy configuration — use it when a policy applies
    /// to all EU member states collectively rather than a specific country.
    /// </remarks>
    public static Region EU { get; } = Region.Create("EU", "EU", isEU: true, isEEA: true, protectionLevel: DataProtectionLevel.High);

    // ---------------------------------------------------------------
    //  Aggregate collections
    // ---------------------------------------------------------------

    /// <summary>
    /// All 27 EU member states.
    /// </summary>
    /// <remarks>
    /// Data flows freely between all EU member states under GDPR Article 1(3).
    /// No adequacy decision, SCCs, or BCRs are needed for intra-EU transfers.
    /// </remarks>
    public static IReadOnlyList<Region> EUMemberStates { get; } =
    [
        AT, BE, BG, HR, CY, CZ, DK, EE, FI, FR, DE, GR, HU, IE, IT,
        LV, LT, LU, MT, NL, PL, PT, RO, SK, SI, ES, SE
    ];

    /// <summary>
    /// All 30 EEA countries (27 EU + Iceland, Liechtenstein, Norway).
    /// </summary>
    /// <remarks>
    /// GDPR applies throughout the EEA. Data flows freely between all EEA
    /// countries without additional transfer mechanisms.
    /// </remarks>
    public static IReadOnlyList<Region> EEACountries { get; } =
    [
        AT, BE, BG, HR, CY, CZ, DK, EE, FI, FR, DE, GR, HU, IE, IT,
        LV, LT, LU, MT, NL, PL, PT, RO, SK, SI, ES, SE,
        IS, LI, NO
    ];

    /// <summary>
    /// Countries with an EU adequacy decision under GDPR Article 45 (non-EEA only).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data can be transferred to these countries without additional authorization.
    /// As of 2025, this list covers 15 countries/territories.
    /// </para>
    /// <para>
    /// Note: EEA countries are not included in this list because they are governed
    /// directly by GDPR, not via adequacy decisions. For a combined list of all
    /// countries to which data can flow freely, combine <see cref="EEACountries"/>
    /// and <see cref="AdequacyCountries"/>.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Region> AdequacyCountries { get; } =
    [
        AD, AR, CA, FO, GG, IL, IM, JP, JE, NZ, KR, CH, GB, UY, US
    ];

    private static readonly Dictionary<string, Region> s_byCode = BuildCodeLookup();

    /// <summary>
    /// Looks up a well-known region by its code (case-insensitive).
    /// </summary>
    /// <param name="code">The region code to look up (e.g., "DE", "US", "JP").</param>
    /// <returns>
    /// The matching <see cref="Region"/> if found in the registry; otherwise, <c>null</c>.
    /// </returns>
    public static Region? GetByCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return s_byCode.GetValueOrDefault(code.ToUpperInvariant());
    }

    private static Dictionary<string, Region> BuildCodeLookup()
    {
        var lookup = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);

        foreach (var region in EUMemberStates)
            lookup.TryAdd(region.Code, region);

        foreach (var region in new[] { IS, LI, NO })
            lookup.TryAdd(region.Code, region);

        foreach (var region in AdequacyCountries)
            lookup.TryAdd(region.Code, region);

        foreach (var region in new[] { AU, BR, CN, IN, SG })
            lookup.TryAdd(region.Code, region);

        lookup.TryAdd(EU.Code, EU);

        return lookup;
    }
}
