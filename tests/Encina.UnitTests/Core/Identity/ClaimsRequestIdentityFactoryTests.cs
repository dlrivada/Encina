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
        // The identity keeps a copy of the authenticated identities, never the caller's object.
        identity.Principal.ShouldNotBeSameAs(principal);
        identity.Principal!.FindFirst("sub")!.Value.ShouldBe("alice");
    }

    [Fact]
    public void CheckSubject_AuthenticatedWithoutSubject_IsMissing_AndLogs162WithTypesOnce()
    {
        var principal = Authenticated(new Claim("email", Sentinel));
        var factory = CreateFactory();

        var refusal = factory.CheckSubject(principal);
        factory.Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);

        refusal.GetValueOrDefault().GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.MissingSubjectClaim);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(162);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain("bearer");
        record.Message.ShouldContain("sub");
        AssertNoSentinelLogged();
    }

    [Fact]
    public void CheckSubject_NullOrUnauthenticatedPrincipal_IsNotRefused()
    {
        var factory = CreateFactory();

        factory.CheckSubject(null).ShouldBeNull();
        factory.CheckSubject(new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", "x")]))).ShouldBeNull();
        factory.CheckSubject(new ClaimsPrincipal()).ShouldBeNull();
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void CheckSubject_BlankOnlySubjectClaims_AreMissing()
    {
        var refusal = CreateFactory().CheckSubject(Authenticated(new Claim("sub", " "), new Claim("oid", "")));

        refusal.GetValueOrDefault().GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.MissingSubjectClaim);
    }

    [Fact]
    public void CheckSubject_AUsableSubject_IsNotRefused_AndLogsNothing()
    {
        CreateFactory().CheckSubject(Authenticated(new Claim("sub", "alice"))).ShouldBeNull();
        _logger.Collector.Count.ShouldBe(0);
    }

    [Fact]
    public void CheckSubject_SubAndOidWithDifferentValuesInOneIdentity_IsNotAConflict_AndSubWins()
    {
        var principal = Authenticated(new Claim("sub", "pairwise"), new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "directory"));
        var factory = CreateFactory();

        factory.CheckSubject(principal).ShouldBeNull();
        factory.Create(principal).UserId.ShouldBe("pairwise");
    }

    [Fact]
    public void CheckSubject_TwoDifferentValuesOfTheWinningTypeInOneIdentity_IsConflicting_AndLogs164()
    {
        var principal = Authenticated(new Claim("sub", "alice-" + Sentinel), new Claim("sub", "bob-" + Sentinel));

        var refusal = CreateFactory().CheckSubject(principal);

        refusal.GetValueOrDefault().GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.ConflictingSubjectClaims);
        _logger.Collector.GetSnapshot().ShouldHaveSingleItem().Id.Id.ShouldBe(164);
        AssertNoSentinelLogged();
    }

    [Fact]
    public void CheckSubject_TheSameValueRepeated_IsNotAConflict()
    {
        CreateFactory().CheckSubject(Authenticated(new Claim("sub", "alice"), new Claim("sub", "alice"))).ShouldBeNull();
    }

    public static TheoryData<string, string> SubjectCauses => new()
    {
        { "service:" + Sentinel, RequestIdentityErrorCodes.ReservedSubjectClaim },
        { "SERVICE:" + Sentinel, RequestIdentityErrorCodes.ReservedSubjectClaim },
        { " service:" + Sentinel, RequestIdentityErrorCodes.ReservedSubjectClaim },
        { Sentinel + " ", RequestIdentityErrorCodes.MalformedSubjectClaim },
        { "a\u0007" + Sentinel, RequestIdentityErrorCodes.MalformedSubjectClaim }
    };

    [Theory]
    [MemberData(nameof(SubjectCauses))]
    public void CheckSubject_ReservedOrMalformedSubject_HasItsOwnCode_AndLogs163WithTheClaimTypeOnly(string subject, string code)
    {
        var factory = CreateFactory();
        var principal = Authenticated(new Claim("sub", subject));

        var refusal = factory.CheckSubject(principal);
        factory.Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);

        refusal.GetValueOrDefault().GetCode().IfNone("none").ShouldBe(code);
        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(163);
        record.Message.ShouldContain("sub");
        AssertNoSentinelLogged();
    }

    [Fact]
    public void CheckSubject_AndCreate_AgreeOnEveryCause()
    {
        var factory = CreateFactory();
        ClaimsPrincipal[] refused =
        [
            Authenticated(new Claim("email", "x")),
            Authenticated(new Claim("sub", "service:x")),
            Authenticated(new Claim("sub", " padded ")),
            Authenticated(new Claim("sub", "a"), new Claim("sub", "b")),
            new([new ClaimsIdentity([new Claim("sub", "a")], "bearer"), new ClaimsIdentity([new Claim("sub", "b")], "cookie")])
        ];
        ClaimsPrincipal[] accepted =
        [
            Authenticated(new Claim("sub", "alice")),
            Authenticated(new Claim("sub", "a"), new Claim("oid", "b")),
            new([new ClaimsIdentity([new Claim("sub", "a")], "bearer"), new ClaimsIdentity([new Claim("email", "e")], "cookie")])
        ];

        foreach (var principal in refused)
        {
            factory.CheckSubject(principal).ShouldNotBeNull();
            factory.Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);
        }

        foreach (var principal in accepted)
        {
            factory.CheckSubject(principal).ShouldBeNull();
            factory.Create(principal).Kind.ShouldBe(IdentityKind.User);
        }
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
        factory.TryResolveTenantId(principal, out var tenantId).ShouldBeTrue();
        tenantId.ShouldBeNull();
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
    public void CheckSubject_TwoAuthenticatedIdentitiesWithDifferentSubjects_IsConflicting_AndLogs164()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice-" + Sentinel)], "bearer"),
            new ClaimsIdentity([new Claim("sub", "bob-" + Sentinel)], "cookie")
        ]);

        CreateFactory().CheckSubject(principal).GetValueOrDefault().GetCode().IfNone("none")
            .ShouldBe(RequestIdentityErrorCodes.ConflictingSubjectClaims);

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
        factory.TryResolveTenantId(principal, out var tenantId).ShouldBeFalse();
        tenantId.ShouldBeNull();

        var record = _logger.Collector.GetSnapshot().ShouldHaveSingleItem();
        record.Id.Id.ShouldBe(164);
        record.Message.ShouldContain("tenant");
        AssertNoSentinelLogged();
    }

    [Theory]
    [InlineData("tenant_id")]
    [InlineData("tid")]
    [InlineData(RequestIdentityOptions.TenantIdentifierClaimType)]
    public void TryResolveTenantId_ReadsTheConfiguredTenantClaims_OfAuthenticatedPrincipals(string claimType)
    {
        CreateFactory().TryResolveTenantId(Authenticated(new Claim("sub", "alice"), new Claim(claimType, "tenant-1")), out var tenantId).ShouldBeTrue();
        tenantId.ShouldBe("tenant-1");
    }

    [Fact]
    public void TryResolveTenantId_TwoDifferentTenantClaimsInOneIdentity_ReportsAConflict()
    {
        var principal = Authenticated(new Claim("sub", "alice"), new Claim("tenant_id", "t1"), new Claim("tenant_id", "t2"));
        var factory = CreateFactory();

        factory.TryResolveTenantId(principal, out var tenantId).ShouldBeFalse();
        tenantId.ShouldBeNull();
        factory.Create(principal).ShouldBeSameAs(RequestIdentity.Anonymous);
    }

    [Fact]
    public void TryResolveTenantId_RepeatedSameTenantClaim_IsNotAConflict()
    {
        var principal = Authenticated(new Claim("sub", "alice"), new Claim("tenant_id", "t1"), new Claim("tenant_id", "t1"));

        CreateFactory().TryResolveTenantId(principal, out var tenantId).ShouldBeTrue();
        tenantId.ShouldBe("t1");
    }

    [Fact]
    public void TryResolveTenantId_HasNoTenant_ForNullOrUnauthenticatedPrincipals()
    {
        var factory = CreateFactory();

        factory.TryResolveTenantId(null, out var nullTenant).ShouldBeTrue();
        factory.TryResolveTenantId(new ClaimsPrincipal(new ClaimsIdentity([new Claim("tenant_id", "t")])), out var unauthenticatedTenant).ShouldBeTrue();
        factory.TryResolveTenantId(Authenticated(new Claim("sub", "alice")), out var noClaimTenant).ShouldBeTrue();
        nullTenant.ShouldBeNull();
        unauthenticatedTenant.ShouldBeNull();
        noClaimTenant.ShouldBeNull();
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
