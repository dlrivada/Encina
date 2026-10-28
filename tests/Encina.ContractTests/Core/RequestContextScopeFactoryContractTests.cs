using System.Security.Claims;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.ContractTests.Core;

/// <summary>
/// The contract of <see cref="IRequestContextScopeFactory"/> as registered by <c>AddEncina</c>:
/// identities come only from the factory and honour <see cref="RequestIdentityOptions"/>, a scope is
/// invalidated when <c>work</c> completes, refusals are results (never exceptions) and never invoke
/// <c>work</c>.
/// </summary>
public sealed class RequestContextScopeFactoryContractTests : IDisposable
{
    private const string Job = "contract-job";

    private readonly ServiceProvider _provider;

    public RequestContextScopeFactoryContractTests()
    {
        var services = new ServiceCollection();
        services.AddEncina();
        services.AddEncinaRequestIdentity(options =>
        {
            options.UserIdClaimTypes.Clear();
            options.UserIdClaimTypes.Add("oid");
            options.TenantIdClaimTypes.Clear();
            options.TenantIdClaimTypes.Add("org");
        });
        services.AddEncinaServiceIdentity(Job, id => id.WithRoles("job"));
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private IRequestContextScopeFactory Scopes => _provider.GetRequiredService<IRequestContextScopeFactory>();

    public void Dispose() => _provider.Dispose();

    private static Task<Either<EncinaError, IRequestContext>> Capture(IRequestContext context, CancellationToken cancellationToken) =>
        Task.FromResult(Right<EncinaError, IRequestContext>(context));

    private static ClaimsPrincipal Principal(params Claim[] claims) => new(new ClaimsIdentity(claims, "contract"));

    // The identity a scope issued, read after the scope ended (Identity then reads Anonymous, #1892).
    private static RequestIdentity Issued(IRequestContext context) => ((RequestContext)context).IssuedIdentity;

    [Fact]
    public async Task RunAsPrincipalAsync_RunInboundAsync_AndRunRestoredAsync_HonourTheConfiguredClaimMap()
    {
        var principal = Principal(new Claim("oid", "user-1"), new Claim("org", "tenant-1"), new Claim("sub", "ignored"));

        var asPrincipal = (await Scopes.RunAsPrincipalAsync(principal, Capture)).ShouldBeSuccess();
        var inbound = (await Scopes.RunInboundAsync(new InboundRequestInfo(principal), Capture)).ShouldBeSuccess();
        var restored = (await Scopes.RunRestoredAsync(
            new PersistedRequestIdentity(IdentityKind.User, "user-1", "tenant-1", "corr", null), PersistedIdentitySource.Internal, Capture)).ShouldBeSuccess();

        Issued(asPrincipal).UserId.ShouldBe("user-1");
        asPrincipal.TenantId.ShouldBe("tenant-1");
        Issued(inbound).UserId.ShouldBe("user-1");
        Issued(restored).UserId.ShouldBe("user-1");
        Issued(restored).HasClaim("oid", "user-1").ShouldBeTrue();
    }

    [Fact]
    public async Task RunInboundAsync_TenantHeader_NeverOutranksTheClaim_AndChoosesTheTenantOnlyWhenTrusted()
    {
        var principal = Principal(new Claim("oid", "user-1"), new Claim("org", "tenant-1"));
        var noClaim = Principal(new Claim("oid", "user-1"));

        var untrusted = (await Scopes.RunInboundAsync(new InboundRequestInfo(noClaim, TenantHeaderValue: "header"), Capture)).ShouldBeSuccess();
        var trusted = (await Scopes.RunInboundAsync(new InboundRequestInfo(noClaim, TenantHeaderValue: "header", TenantHeaderTrusted: true), Capture)).ShouldBeSuccess();
        var agreeing = (await Scopes.RunInboundAsync(new InboundRequestInfo(principal, TenantHeaderValue: "tenant-1"), Capture)).ShouldBeSuccess();
        var disagreeing = await Scopes.RunInboundAsync(new InboundRequestInfo(principal, TenantHeaderValue: "header", TenantHeaderTrusted: true), Capture);

        untrusted.TenantId.ShouldBeNull();
        trusted.TenantId.ShouldBe("header");
        agreeing.TenantId.ShouldBe("tenant-1");
        disagreeing.IsLeft.ShouldBeTrue();
        disagreeing.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.InboundTenantConflict));
    }

    [Fact]
    public async Task RunAsPrincipalAsync_ConflictingTenantClaims_AreRefusedWithTheirOwnCode_AndNeverInvokeWork()
    {
        var invoked = false;
        var principal = Principal(new Claim("oid", "user-1"), new Claim("org", "tenant-1"), new Claim("org", "tenant-2"));

        var result = await Scopes.RunAsPrincipalAsync(principal, (context, ct) =>
        {
            invoked = true;
            return Capture(context, ct);
        });

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.ConflictingTenantClaims));
        invoked.ShouldBeFalse();
    }

    [Theory]
    [InlineData("missing", RequestIdentityErrorCodes.MissingSubjectClaim)]
    [InlineData("malformed", RequestIdentityErrorCodes.MalformedSubjectClaim)]
    [InlineData("reserved", RequestIdentityErrorCodes.ReservedSubjectClaim)]
    [InlineData("conflicting", RequestIdentityErrorCodes.ConflictingSubjectClaims)]
    public async Task AnAuthenticatedPrincipalWithAnUnusableSubject_IsRefusedWithItsCode_OnBothPaths_AndNeverInvokesWork(string cause, string code)
    {
        var invoked = false;
        var principal = cause switch
        {
            "missing" => Principal(new Claim("sub", "ignored-by-the-map")),
            "malformed" => Principal(new Claim("oid", " padded ")),
            "reserved" => Principal(new Claim("oid", "service:job")),
            _ => Principal(new Claim("oid", "user-1"), new Claim("oid", "user-2"))
        };

        var asPrincipal = await Scopes.RunAsPrincipalAsync(principal, (context, ct) =>
        {
            invoked = true;
            return Capture(context, ct);
        });
        var inbound = await Scopes.RunInboundAsync(new InboundRequestInfo(principal), (context, ct) =>
        {
            invoked = true;
            return Capture(context, ct);
        });

        asPrincipal.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
        inbound.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
        asPrincipal.IsLeft.ShouldBeTrue();
        inbound.IsLeft.ShouldBeTrue();
        invoked.ShouldBeFalse();
    }

    [Fact]
    public async Task ABadSubject_WinsOverABadTenant_OnBothPaths_AndAnUnauthenticatedPrincipalStaysAnonymous()
    {
        var bad = Principal(new Claim("sub", "ignored-by-the-map"), new Claim("org", "t1"), new Claim("org", "t2"));
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity([new Claim("oid", "user-1")]));

        var asPrincipal = await Scopes.RunAsPrincipalAsync(bad, Capture);
        var inbound = await Scopes.RunInboundAsync(new InboundRequestInfo(bad), Capture);
        var anonymousPrincipal = (await Scopes.RunAsPrincipalAsync(unauthenticated, Capture)).ShouldBeSuccess();
        var anonymousInbound = (await Scopes.RunInboundAsync(new InboundRequestInfo(unauthenticated), Capture)).ShouldBeSuccess();

        asPrincipal.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.MissingSubjectClaim));
        inbound.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(RequestIdentityErrorCodes.MissingSubjectClaim));
        asPrincipal.IsLeft.ShouldBeTrue();
        inbound.IsLeft.ShouldBeTrue();
        Issued(anonymousPrincipal).Kind.ShouldBe(IdentityKind.Anonymous);
        Issued(anonymousInbound).Kind.ShouldBe(IdentityKind.Anonymous);
    }

    [Fact]
    public async Task ABadSubject_InsideAUserScope_IsStillRefusedWithTheSubjectCode_OnThePrincipalPath()
    {
        var user = Principal(new Claim("oid", "alice"));
        var bad = Principal(new Claim("sub", "x"));

        var innerCode = "not-run";

        var outcome = await Scopes.RunAsPrincipalAsync(user, async (_, ct) =>
        {
            var inner = await Scopes.RunAsPrincipalAsync(bad, Capture, cancellationToken: ct);
            innerCode = inner.Match(Right: _ => "opened", Left: error => error.GetCode().IfNone("none"));
            return Right<EncinaError, int>(1);
        });

        outcome.IsRight.ShouldBeTrue();
        innerCode.ShouldBe(RequestIdentityErrorCodes.MissingSubjectClaim);
    }

    [Fact]
    public async Task EveryScope_IsInvalidatedWhenWorkCompletes()
    {
        var service = (await Scopes.RunAsServiceAsync(Job, Capture)).ShouldBeSuccess();
        var principal = (await Scopes.RunAsPrincipalAsync(Principal(new Claim("oid", "u")), Capture)).ShouldBeSuccess();

        Issued(service).Issuer!.IsLive.ShouldBeFalse();
        Issued(principal).Issuer!.IsLive.ShouldBeFalse();
        service.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        principal.Identity.ShouldBeSameAs(RequestIdentity.Anonymous);
        _provider.GetRequiredService<IRequestContextAccessor>().RequestContext.ShouldBeNull();
    }

    [Fact]
    public async Task Refusals_AreResults_AndNeverInvokeWork()
    {
        var invoked = 0;
        Task<Either<EncinaError, int>> Work(IRequestContext context, CancellationToken cancellationToken)
        {
            invoked++;
            return Task.FromResult(Right<EncinaError, int>(1));
        }

        var unknown = await Scopes.RunAsServiceAsync("not-declared", Work);
        var reserved = await Scopes.RunAsServiceAsync("encina.anything", Work);
        var invalid = await Scopes.RunRestoredAsync(new PersistedRequestIdentity(IdentityKind.User, "service:x", null, "c", null), PersistedIdentitySource.Internal, Work);
        var nested = await Scopes.RunAsPrincipalAsync(Principal(new Claim("oid", "u")), (_, ct) => Scopes.RunAsServiceAsync(Job, Work, cancellationToken: ct));

        var unknownTaskOverload = await Scopes.RunAsServiceAsync("not-declared", (_, _) =>
        {
            invoked++;
            return Task.CompletedTask;
        });

        unknownTaskOverload.IsLeft.ShouldBeTrue();
        unknown.IsLeft.ShouldBeTrue();
        reserved.IsLeft.ShouldBeTrue();
        invalid.IsLeft.ShouldBeTrue();
        nested.IsLeft.ShouldBeTrue();
        invoked.ShouldBe(0);
    }
}
