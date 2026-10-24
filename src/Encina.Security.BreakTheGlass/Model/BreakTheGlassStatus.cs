namespace Encina.Security.BreakTheGlass;

/// <summary>
/// The lifecycle status of a break-the-glass elevation.
/// </summary>
/// <remarks>
/// The numeric values are persisted by the provider stores and must not change.
/// </remarks>
public enum BreakTheGlassStatus
{
    /// <summary>The elevation is open; it only takes effect while its expiry has not passed.</summary>
    Active = 0,

    /// <summary>The elevation passed its expiry and was closed by the expiry sweeper.</summary>
    Expired = 1,

    /// <summary>The elevation was closed before its expiry, by revocation or by an unjustified review.</summary>
    Revoked = 2
}
