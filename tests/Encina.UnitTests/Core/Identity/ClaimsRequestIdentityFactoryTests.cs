using System.Security.Claims;
using Microsoft.Extensions.Logging.Testing;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for <see cref="ClaimsRequestIdentityFactory"/>: the one claim map, the fail-closed
/// mappings to anonymous with their warnings, and that no claim value reaches a log.
/// </summary>
public sealed class ClaimsRequestIdentityFactoryTests
{
    private const string Sentinel = "sentinel-7a1f";

    private readonly FakeLogger<ClaimsRequestIdentityFactory> _logger = new();

    private ClaimsRequestIdentityFactory CreateFactory(Action<RequestIdentityOptions>? configure = null)
    {
        var options = new RequestIdentityOptions();
        configure?.Invoke(options);
        return new ClaimsRequestIdentityFactory(Options.Create(options), _logger);
    }

    private static ClaimsPrincipal Authenticated(params Claim[] claims)
        => new(new ClaimsIdentity(claims, "bearer"));

    [Fact]
    public void Create_NullPrincipal_IsAnonymous()
    {
        CreateFactory().Create(null).ShouldBeSameAs(RequestIdentity.Anonymous);
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void Create_UnauthenticatedPrincipalWithSubject_IsAnonymous()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")]));

        CreateFactory().Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    [Fact]
    public void Create_AuthenticatedWithSubject_IsAUserCarryingThePrincipal()
    {
        var principal = Authenticated(new Claim("sub", "alice"));

        var identity = CreateFactory().Create(principal);

        identity.Kind.ShouldBe(IdentityKind.User);
        identity.UserId.ShouldBe("alice");
        identity.Principal.ShouldBeSameAs(principal);
    }

    [Fact]
    public void Create_AuthenticatedWithoutSubject_IsAnonymous_AndLogs162WithTypesOnly()
    {
        var principal = Authenticated(new Claim("email", Sentinel));

        var identity = CreateFactory().Create(principal);

        identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(162);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("bearer");
        record.Message.ShouldContain("sub");
        AssertNoSentinelLogged();
    }

    [Theory]
    [InlineData("service:" + Sentinel)]
    [InlineData("SERVICE:" + Sentinel)]
    [InlineData(" service:" + Sentinel)]
    [InlineData(Sentinel + " ")]
    [InlineData("a\u0007" + Sentinel)]
    public void Create_ReservedOrMalformedSubject_IsAnonymous_AndLogs163WithTheClaimTypeOnly(string subject)
    {
        var identity = CreateFactory().Create(Authenticated(new Claim("sub", subject)));

        identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(163);
        record.Message.ShouldContain("sub");
        AssertNoSentinelLogged();
    }

    [Fact]
    public void Create_SubjectPrecedence_IsSubThenNameIdentifierThenObjectId()
    {
        var factory = CreateFactory();

        factory.Create(Authenticated(
            new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "oid-user"),
            new Claim(ClaimTypes.NameIdentifier, "nameid-user"),
            new Claim("sub", "sub-user"))).UserId.ShouldBe("sub-user");
        factory.Create(Authenticated(
            new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "oid-user"),
            new Claim(ClaimTypes.NameIdentifier, "nameid-user"))).UserId.ShouldBe("nameid-user");
        factory.Create(Authenticated(
            new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "oid-user"))).UserId.ShouldBe("oid-user");
    }

    [Fact]
    public void Create_HonoursACustomSubjectOrder()
    {
        var factory = CreateFactory(options =>
        {
            options.UserIdClaimTypes.Clear();
            options.UserIdClaimTypes.Add("uid");
        });

        factory.Create(Authenticated(new Claim("sub", "sub-user"), new Claim("uid", "custom-user"))).UserId.ShouldBe("custom-user");
    }

    [Fact]
    public void Create_SkipsBlankSubjectClaims()
    {
        var identity = CreateFactory().Create(Authenticated(new Claim("sub", " "), new Claim(ClaimTypes.NameIdentifier, "nameid-user")));

        identity.UserId.ShouldBe("nameid-user");
    }

    [Fact]
    public void Create_CollectsRoles_FromConfiguredTypesAndTheIdentityRoleClaimType()
    {
        var claimsIdentity = new ClaimsIdentity(
            [new Claim("sub", "alice"), new Claim("role", "reader"), new Claim(ClaimTypes.Role, "writer"), new Claim("grp", "auditor")],
            "bearer",
            nameType: null,
            roleType: "grp");

        var identity = CreateFactory().Create(new ClaimsPrincipal(claimsIdentity));

        identity.Roles.ShouldBe(["reader", "writer", "auditor"], ignoreOrder: true);
    }

    [Fact]
    public void Create_IgnoresTheIdentityRoleClaimType_WhenDisabled()
    {
        var claimsIdentity = new ClaimsIdentity(
            [new Claim("sub", "alice"), new Claim("grp", "auditor")], "bearer", nameType: null, roleType: "grp");

        var identity = CreateFactory(options => options.IncludeIdentityRoleClaimType = false).Create(new ClaimsPrincipal(claimsIdentity));

        identity.Roles.ShouldBeEmpty();
    }

    [Fact]
    public void Create_SplitsPermissions_WhenASeparatorIsSet()
    {
        var principal = Authenticated(new Claim("sub", "alice"), new Claim("scope", "orders:read  orders:write"));

        var identity = CreateFactory(options =>
        {
            options.PermissionClaimTypes.Add("scope");
            options.PermissionClaimSeparator = ' ';
        }).Create(principal);

        identity.Permissions.ShouldBe(["orders:read", "orders:write"], ignoreOrder: true);
    }

    [Fact]
    public void Create_KeepsAPermissionWhole_WithoutASeparator()
    {
        var identity = CreateFactory().Create(Authenticated(new Claim("sub", "alice"), new Claim("permission", "a b")));

        identity.Permissions.ShouldBe(["a b"]);
    }

    [Fact]
    public void Create_IgnoresUnauthenticatedIdentities_ForSubjectRolesPermissionsAndTenant()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice")], "bearer"),
            new ClaimsIdentity([new Claim("sub", "mallory"), new Claim("role", "admin"), new Claim("permission", "all"), new Claim("tenant_id", "other")])
        ]);
        var factory = CreateFactory();

        var identity = factory.Create(principal);

        identity.UserId.ShouldBe("alice");
        identity.Roles.ShouldBeEmpty();
        identity.Permissions.ShouldBeEmpty();
        factory.ResolveTenantId(principal).ShouldBeNull();
    }

    [Fact]
    public void Create_TwoAuthenticatedIdentitiesWithTheSameSubject_IsThatUser()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice"), new Claim("role", "reader")], "bearer"),
            new ClaimsIdentity([new Claim("sub", "alice"), new Claim("role", "writer")], "cookie")
        ]);

        var identity = CreateFactory().Create(principal);

        identity.UserId.ShouldBe("alice");
        identity.Roles.ShouldBe(["reader", "writer"], ignoreOrder: true);
    }

    [Fact]
    public void Create_TwoAuthenticatedIdentitiesWithDifferentSubjects_IsAnonymous_AndLogs164()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice-" + Sentinel)], "bearer"),
            new ClaimsIdentity([new Claim("sub", "bob-" + Sentinel)], "cookie")
        ]);

        CreateFactory().Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(164);
        record.Message.ShouldContain("subject");
        AssertNoSentinelLogged();
    }

    [Fact]
    public void Create_TwoAuthenticatedIdentitiesWithDifferentTenants_IsAnonymous_AndLogs164()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice"), new Claim("tenant_id", "t1-" + Sentinel)], "bearer"),
            new ClaimsIdentity([new Claim("tid", "t2-" + Sentinel)], "cookie")
        ]);
        var factory = CreateFactory();

        factory.Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);
        factory.ResolveTenantId(principal).ShouldBeNull();

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(164);
        record.Message.ShouldContain("tenant");
        AssertNoSentinelLogged();
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    [InlineData(RequestIdentityOptions.TenantIdentifierClaimType)]
    public void ResolveTenantId_ReadsTheConfiguredTenantClaims_OfAuthenticatedPrincipals(string claimType)
    {
        CreateFactory().ResolveTenantId(Authenticated(new Claim("sub", "alice"), new Claim(claimType, "tenant-1"))).ShouldBe("tenant-1");
    }

    [Fact]
    public void ResolveTenantId_IsNull_ForNullOrUnauthenticatedPrincipals()
    {
        var factory = CreateFactory();

        factory.ResolveTenantId(null).ShouldBeNull();
        factory.ResolveTenantId(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "t")]))).ShouldBeNull();
        factory.ResolveTenantId(Authenticated(new Claim("sub", "alice"))).ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithoutLogger_UsesANullLogger()
    {
        var factory = new ClaimsRequestIdentityFactory(Options.Create(new RequestIdentityOptions()));

        factory.Create(Authenticated(new Claim("email", "x"))).ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    [Fact]
    public void Constructor_RejectsNullOptions()
    {
        Should.Throw<ArgumentNullException>(() => new ClaimsRequestIdentityFactory(null!));
    }

    private void AssertNoSentinelLogged()
    {
        foreach (var record in _logger.Collector.GetSnapshot())
        {
            record.Message.ShouldNotContain(Sentinel);
            record.StructuredState?.ShouldAllBe(pair => pair.Value == null || !pair.Value.Contains(Sentinel));
        }
    }
}
