using System.Security.Claims;

namespace Encina.Testing.Identity;

/// <summary>
/// Builds <see cref="RequestIdentity"/> instances for tests.
/// </summary>
/// <remarks>
/// <para>
/// Production code cannot mint an authenticated identity: Encina's entry points create them,
/// validated and logged. This class is the declared test seam (ADR-035) that application tests use
/// instead. A user built here carries an authenticated principal (authentication type
/// <see cref="AuthenticationType"/>) with a <c>sub</c> claim, one <see cref="ClaimTypes.Role"/>
/// claim per role and one <c>permission</c> claim per permission, so
/// <see cref="RequestIdentity.HasClaim"/> works as it does for a mapped principal.
/// </para>
/// <para>
/// Service identities are declared at startup and opened through identity scopes; their test
/// helper arrives with the scope API.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = TestRequestContext.For(TestIdentity.User("alice", roles: ["admin"]));
/// var result = await behavior.Handle(request, context, next, CancellationToken.None);
/// </code>
/// </example>
public static class TestIdentity
{
    /// <summary>
    /// The authentication type of the principals built by <see cref="User"/>.
    /// </summary>
    public const string AuthenticationType = "encina-test";

    /// <summary>
    /// Gets the anonymous identity.
    /// </summary>
    public static RequestIdentity Anonymous => RequestIdentity.Anonymous;

    /// <summary>
    /// Builds an authenticated user identity.
    /// </summary>
    /// <param name="userId">The user id. It must follow the user-id rule: not blank, no surrounding whitespace or control characters, no <c>service:</c> prefix.</param>
    /// <param name="roles">The user's roles.</param>
    /// <param name="permissions">The user's permissions.</param>
    /// <param name="claims">Extra claims for the principal (for <c>[RequireClaim]</c>-style checks).</param>
    /// <returns>A <see cref="IdentityKind.User"/> identity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userId"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="userId"/> breaks the user-id rule.</exception>
    public static RequestIdentity User(
        string userId,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        IEnumerable<Claim>? claims = null)
    {
        ArgumentNullException.ThrowIfNull(userId);

        string[] roleList = [.. roles ?? []];
        string[] permissionList = [.. permissions ?? []];

        List<Claim> principalClaims = [new Claim("sub", userId)];
        principalClaims.AddRange(roleList.Select(static role => new Claim(ClaimTypes.Role, role)));
        principalClaims.AddRange(permissionList.Select(static permission => new Claim("permission", permission)));
        principalClaims.AddRange(claims ?? []);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(principalClaims, AuthenticationType));
        return RequestIdentity.ForUser(userId, principal, roleList, permissionList);
    }
}
