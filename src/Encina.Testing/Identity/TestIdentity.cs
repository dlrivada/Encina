using System.Security.Claims;

namespace Encina.Testing.Identity;

/// <summary>
/// Builds <see cref="RequestIdentity"/> instances and principals for tests.
/// </summary>
/// <remarks>
/// <para>
/// Production code cannot mint an authenticated identity: Encina's identity scopes create them,
/// validated and logged. This class is the declared test seam (ADR-035) that application tests use
/// instead. It only <b>builds</b>: an identity built here has no issuing scope, so it serves
/// handlers, behaviors and gates called directly. A test that <b>dispatches</b> with an identity,
/// or needs one ambient, binds it through <see cref="IRequestContextScopeFactory"/>:
/// <see cref="IRequestContextScopeFactory.RunAsPrincipalAsync{T}"/> with a <see cref="Principal"/>, or
/// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/> with a service the test host declares.
/// </para>
/// <para>
/// A user built here carries an authenticated principal (authentication type
/// <see cref="AuthenticationType"/>) with a <c>sub</c> claim, one <see cref="ClaimTypes.Role"/>
/// claim per role and one <c>permission</c> claim per permission, so
/// <see cref="RequestIdentity.HasClaim"/> works as it does for a mapped principal.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A behavior called directly
/// var context = TestRequestContext.For(TestIdentity.User("alice", roles: ["admin"]));
/// var result = await behavior.Handle(request, context, next, CancellationToken.None);
///
/// // A dispatch: bind the identity through the scope factory
/// var outcome = await scopes.RunAsPrincipalAsync(
///     TestIdentity.Principal("alice", roles: ["admin"]),
///     (scopeContext, ct) => encina.Send(new ApproveInvoice(), ct));
/// </code>
/// </example>
public static class TestIdentity
{
    /// <summary>
    /// The authentication type of the principals built by <see cref="User"/> and <see cref="Principal"/>.
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
    /// <returns>A <see cref="IdentityKind.User"/> identity with no issuing scope.</returns>
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
        return RequestIdentity.ForUser(userId, Principal(userId, roleList, permissionList, claims), roleList, permissionList);
    }

    /// <summary>
    /// Builds an authenticated principal that the default claim map (<see cref="RequestIdentityOptions"/>)
    /// maps to a user with <paramref name="userId"/>, <paramref name="roles"/> and
    /// <paramref name="permissions"/>, for <see cref="IRequestContextScopeFactory.RunAsPrincipalAsync{T}"/>.
    /// </summary>
    /// <param name="userId">The value of the <c>sub</c> claim.</param>
    /// <param name="roles">One <see cref="ClaimTypes.Role"/> claim each.</param>
    /// <param name="permissions">One <c>permission</c> claim each.</param>
    /// <param name="claims">Extra claims.</param>
    /// <returns>A principal with one authenticated identity of type <see cref="AuthenticationType"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userId"/> is <see langword="null"/>.</exception>
    public static ClaimsPrincipal Principal(
        string userId,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        IEnumerable<Claim>? claims = null)
    {
        ArgumentNullException.ThrowIfNull(userId);

        List<Claim> principalClaims = [new Claim("sub", userId)];
        principalClaims.AddRange((roles ?? []).Select(static role => new Claim(ClaimTypes.Role, role)));
        principalClaims.AddRange((permissions ?? []).Select(static permission => new Claim("permission", permission)));
        principalClaims.AddRange(claims ?? []);

        return new ClaimsPrincipal(new ClaimsIdentity(principalClaims, AuthenticationType));
    }

    /// <summary>
    /// Builds a service identity as a declared service would carry it (subject
    /// <c>service:&lt;name&gt;</c>), for handlers and gates called directly.
    /// </summary>
    /// <param name="name">The service name (pattern <c>^[a-z0-9][a-z0-9.-]{0,62}$</c>).</param>
    /// <param name="roles">The declared roles.</param>
    /// <param name="permissions">The declared permissions.</param>
    /// <returns>A <see cref="IdentityKind.Service"/> identity with no issuing scope.</returns>
    /// <remarks>
    /// To run a dispatch as a service, declare it in the test host with
    /// <c>AddEncinaServiceIdentity</c> and open it with
    /// <see cref="IRequestContextScopeFactory.RunAsServiceAsync{T}"/>.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="name"/> breaks the name pattern, or a role or permission is blank.</exception>
    public static RequestIdentity Service(
        string name,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        if (!ServiceIdentityCatalogOptionsValidator.IsValidName(name))
        {
            throw new ArgumentException("A service identity name must match ^[a-z0-9][a-z0-9.-]{0,62}$.", nameof(name));
        }

        var definition = new ServiceIdentityBuilder()
            .WithRoles([.. roles ?? []])
            .WithPermissions([.. permissions ?? []])
            .Build(name, ServiceIdentityCatalogOptionsValidator.IsReservedName(name));

        return RequestIdentity.ForService(definition);
    }
}
