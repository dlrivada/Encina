using System.Security.Claims;
using Encina.Testing.Identity;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Unit tests for <see cref="RequestIdentity"/>: the invariant, the cached anonymous identity, the
/// user-id rule, claims, the persisted form and redaction.
/// </summary>
public sealed class RequestIdentityTests
{
    [Fact]
    public void Anonymous_IsCached_AndCarriesNothing()
    {
        var anonymous = RequestIdentity.Anonymous;

        anonymous.ShouldBeSameAs(RequestIdentity.Anonymous);
        anonymous.Kind.ShouldBe(IdentityKind.Anonymous);
        anonymous.IsAuthenticated.ShouldBeFalse();
        anonymous.UserId.ShouldBeNull();
        anonymous.Principal.ShouldBeNull();
        anonymous.Roles.ShouldBeEmpty();
        anonymous.Permissions.ShouldBeEmpty();
    }

    [Fact]
    public void ForUser_IsAuthenticated_WithItsUserId()
    {
        var identity = RequestIdentity.ForUser("alice");

        identity.Kind.ShouldBe(IdentityKind.User);
        identity.IsAuthenticated.ShouldBeTrue();
        identity.UserId.ShouldBe("alice");
        identity.Principal.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" alice")]
    [InlineData("alice ")]
    [InlineData("al\u0001ice")]
    [InlineData("service:billing")]
    [InlineData("SERVICE:billing")]
    [InlineData(" service:billing")]
    [InlineData("service:")]
    public void ForUser_RejectsIdsThatBreakTheUserIdRule(string userId)
    {
        Should.Throw<ArgumentException>(() => RequestIdentity.ForUser(userId));
        RequestIdentity.IsValidUserId(userId).ShouldBeFalse();
    }

    [Fact]
    public void IsValidUserId_RejectsNull()
    {
        RequestIdentity.IsValidUserId(null).ShouldBeFalse();
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("user:service")]
    public void IsValidUserId_AcceptsOrdinaryIds(string userId)
    {
        RequestIdentity.IsValidUserId(userId).ShouldBeTrue();
    }

    [Fact]
    public void RolesAndPermissions_AreCaseInsensitive_AndIgnoreBlankEntries()
    {
        var identity = RequestIdentity.ForUser("alice", roles: ["Admin", " ", "reader"], permissions: ["Orders:Read"]);

        identity.Roles.ShouldContain("admin");
        identity.Roles.ShouldContain("READER");
        identity.Roles.Count.ShouldBe(2);
        identity.Permissions.ShouldContain("orders:read");
    }

    [Fact]
    public void HasClaim_CountsOnlyAuthenticatedIdentities()
    {
        var principal = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("dept", "sales")], "bearer"),
            new ClaimsIdentity([new Claim("level", "gold")])
        ]);
        var identity = RequestIdentity.ForUser("alice", principal);

        identity.HasClaim("dept").ShouldBeTrue();
        identity.HasClaim("DEPT", "sales").ShouldBeTrue();
        identity.HasClaim("dept", "SALES").ShouldBeFalse();
        identity.HasClaim("level").ShouldBeFalse();
    }

    [Fact]
    public void HasClaim_WithoutPrincipal_IsFalse()
    {
        RequestIdentity.Anonymous.HasClaim("sub").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void HasClaim_RejectsABlankType(string? type)
    {
        Should.Throw<ArgumentException>(() => RequestIdentity.Anonymous.HasClaim(type!));
    }

    [Fact]
    public void ToPersisted_ForAUser_CarriesTheActorButNoRolesOrPermissions()
    {
        var identity = TestIdentity.User("alice", roles: ["admin"], permissions: ["orders:write"]);

        var persisted = identity.ToPersisted("tenant-1", "corr-1", "cause-1");

        persisted.ShouldBe(new PersistedRequestIdentity(IdentityKind.User, "alice", "tenant-1", "corr-1", "cause-1"));
    }

    [Fact]
    public void ToPersisted_ForAnonymous_HasNoActor()
    {
        var persisted = RequestIdentity.Anonymous.ToPersisted(null, "corr-1", null);

        persisted.Kind.ShouldBe(IdentityKind.Anonymous);
        persisted.ActorId.ShouldBeNull();
        persisted.TenantId.ShouldBeNull();
        persisted.CausationId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ToPersisted_RejectsABlankCorrelationId(string? correlationId)
    {
        Should.Throw<ArgumentException>(() => RequestIdentity.Anonymous.ToPersisted(null, correlationId!, null));
    }

    [Fact]
    public void ToString_PrintsTheKindOnly()
    {
        var text = TestIdentity.User("sentinel-user-9f2c", roles: ["sentinel-role"]).ToString();

        text.ShouldBe("RequestIdentity { Kind = User }");
        text.ShouldNotContain("sentinel");
    }
}
