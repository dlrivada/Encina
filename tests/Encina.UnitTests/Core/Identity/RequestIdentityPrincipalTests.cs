using System.Security.Claims;
using Encina.Testing.Identity;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// The stored principal (D4, minor 8) and the per-token comparison of <c>IsSameAs</c> (Q2, PR #1862
/// review finding 13).
/// </summary>
public sealed class RequestIdentityPrincipalTests
{
    private sealed class AuthenticatedWithoutType(IEnumerable<Claim> claims) : ClaimsIdentity(claims)
    {
        public override bool IsAuthenticated => true;
    }

    private sealed class GroupAnsweringPrincipal(ClaimsIdentity identity) : ClaimsPrincipal(identity)
    {
        public override bool IsInRole(string role) => role == @"DOMAIN\Group" || base.IsInRole(role);
    }

    private static ClaimsPrincipal Principal(params ClaimsIdentity[] identities) => new(identities);

    [Fact]
    public void AnUnauthenticatedIdentity_IsAbsentFromThePrincipal()
    {
        var caller = Principal(
            new ClaimsIdentity([new Claim("sub", "alice")], "bearer"),
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "added-by-transformation")]));

        var principal = RequestIdentity.ForUser("alice", caller).Principal!;

        principal.Identities.Count().ShouldBe(1);
        principal.IsInRole("added-by-transformation").ShouldBeFalse();
    }

    [Fact]
    public void ASubclassIdentity_IsStoredAsAPlainClaimsIdentity_WithoutActorOrBootstrapContext()
    {
        var identity = new AuthenticatedWithoutType([new Claim("sub", "alice")])
        {
            Actor = new ClaimsIdentity([new Claim("sub", "delegate")], "actor"),
            BootstrapContext = "raw-token"
        };

        var stored = RequestIdentity.ForUser("alice", Principal(identity)).Principal!.Identities.Single();

        stored.GetType().ShouldBe(typeof(ClaimsIdentity));
        stored.Actor.ShouldBeNull();
        stored.BootstrapContext.ShouldBeNull();
    }

    [Fact]
    public void AnAuthenticatedIdentityWithoutAType_GetsTheFallbackType_AndStaysAuthenticated()
    {
        var stored = RequestIdentity.ForUser("alice", Principal(new AuthenticatedWithoutType([new Claim("sub", "alice")]))).Principal!;

        stored.Identity!.AuthenticationType.ShouldBe(RequestIdentity.AuthenticatedFallbackType);
        stored.Identity.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public void AGroupNameRoleCheck_DoesNotMatchOnTheCopy_WhileARoleClaimDoes()
    {
        var caller = new GroupAnsweringPrincipal(new ClaimsIdentity([new Claim("sub", "alice"), new Claim(ClaimTypes.Role, "reader")], "windows"));
        caller.IsInRole(@"DOMAIN\Group").ShouldBeTrue();

        var copy = RequestIdentity.ForUser("alice", caller).Principal!;

        copy.IsInRole(@"DOMAIN\Group").ShouldBeFalse();
        copy.IsInRole("reader").ShouldBeTrue();
    }

    [Fact]
    public void ABuilderIdentityAndAFactoryIdentity_WithTheSameClaims_AreTheSame_UnderDefaultAndCustomPerTokenTypes()
    {
        var custom = new RequestIdentityOptions();
        custom.PerTokenClaimTypes.Add("session_nonce");
        var principal = TestIdentity.Principal("alice", roles: ["r"], claims: [new Claim("exp", "1"), new Claim("session_nonce", "a")]);

        foreach (var options in new[] { new RequestIdentityOptions(), custom })
        {
            var factory = new ClaimsRequestIdentityFactory(Microsoft.Extensions.Options.Options.Create(options));
            var fromFactory = factory.Create(principal);
            var fromBuilder = TestIdentity.User("alice", roles: ["r"], claims: [new Claim("exp", "2"), new Claim("session_nonce", "b")]);

            // The union of both identities' per-token sets is removed before comparing.
            fromFactory.IsSameAs(fromBuilder).ShouldBe(ReferenceEquals(options, custom));
            fromBuilder.IsSameAs(fromFactory).ShouldBe(ReferenceEquals(options, custom));
        }
    }

    [Fact]
    public void ClaimTypesCompareCaseInsensitively_AndValuesOrdinally()
    {
        var a = TestIdentity.User("alice", claims: [new Claim("Department", "finance")]);

        a.IsSameAs(TestIdentity.User("alice", claims: [new Claim("department", "finance")])).ShouldBeTrue();
        a.IsSameAs(TestIdentity.User("alice", claims: [new Claim("department", "Finance")])).ShouldBeFalse();
    }

    [Fact]
    public void ForService_RejectsNullAndBlankArguments()
    {
        var definition = new ServiceIdentityBuilder().Build("job", false);

        Should.Throw<ArgumentNullException>(() => RequestIdentity.ForService(null!));
        Should.Throw<ArgumentException>(() => RequestIdentity.ForService(definition, roleClaimType: " "));
        Should.Throw<ArgumentException>(() => RequestIdentity.ForService(definition, permissionClaimType: ""));
    }

    [Fact]
    public void AnIssuer_IsBoundOnce_AndIsNotLiveBeforeBinding()
    {
        var issuer = new IdentityIssuer();
        issuer.IsLive.ShouldBeFalse();
        var holder = new RequestContextAccessor.ContextHolder(null, null, isScope: true);

        issuer.Bind(holder);

        issuer.IsLive.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => issuer.Bind(holder));
        Should.Throw<ArgumentNullException>(() => new IdentityIssuer().Bind(null!));
        holder.Invalidate();
        issuer.IsLive.ShouldBeFalse();
    }

    [Fact]
    public void InboundRequestInfo_PrintsNoValues_AndForCircuitRequiresACorrelationId()
    {
        var info = InboundRequestInfo.ForCircuit(null, "circuit-1");

        info.CorrelationId.ShouldBe("circuit-1");
        info.ToString().ShouldBe("InboundRequestInfo");
        Should.Throw<ArgumentException>(() => InboundRequestInfo.ForCircuit(null, " "));
    }
}
