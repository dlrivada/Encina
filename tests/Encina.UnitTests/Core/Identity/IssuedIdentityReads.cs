namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Reads the identity a scope issued from a context captured inside it, after the scope has ended.
/// </summary>
/// <remarks>
/// <see cref="RequestContext.Identity"/> reads as <see cref="RequestIdentity.Anonymous"/> once the
/// issuing scope has ended (#1892), so a test that returns the scope's context and inspects it
/// afterwards asserts on what the scope issued through <see cref="Issued"/>.
/// </remarks>
internal static class IssuedIdentityReads
{
    /// <summary>Gets the identity <paramref name="context"/> carries as issued, without the liveness check.</summary>
    public static RequestIdentity Issued(this IRequestContext? context) =>
        RequestContext.IssuedIdentityOf(context) ?? RequestIdentity.Anonymous;
}
