namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The names break-the-glass writes into the read audit trail, for queries and reports.
/// </summary>
public static class BreakTheGlassReadAudit
{
    /// <summary>The read-audit purpose set for every elevated request.</summary>
    public const string Purpose = "break-the-glass";

    /// <summary>The read-audit metadata key that holds the elevation id.</summary>
    public const string ElevationIdMetadataKey = "elevation_id";

    /// <summary>The read-audit metadata key that keeps a purpose the application declared before the elevation.</summary>
    public const string DeclaredPurposeMetadataKey = "declared_purpose";
}
