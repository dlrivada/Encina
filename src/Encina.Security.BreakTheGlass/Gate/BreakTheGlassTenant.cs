namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The one notion of "no tenant": a null, empty or whitespace tenant id means tenancy is off, and the store
/// only ever sees <see langword="null"/> for it.
/// </summary>
internal static class BreakTheGlassTenant
{
    public static string? Normalise(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
}
