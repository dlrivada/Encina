using System.Security.Claims;
using Encina.Testing;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// An authenticated principal whose subject is missing, malformed, reserved or conflicting is refused
/// with a typed code on both scope paths and never opened as anonymous (#2855); an unauthenticated
/// principal keeps the anonymous scope.
/// </summary>
public sealed class RequestContextScopeFactorySubjectTests
{
    private const string Sentinel = "sentinel-2855";

    private readonly ScopeTestHost _host = new();

    private static ClaimsPrincipal One(params Claim[] claims) => new(new ClaimsIdentity(claims, "bearer"));

    public static TheoryData<string, string> Causes => new()
    {
        { "missing", RequestIdentityErrorCodes.MissingSubjectClaim },
        { "malformed", RequestIdentityErrorCodes.MalformedSubjectClaim },
        { "reserved", RequestIdentityErrorCodes.ReservedSubjectClaim },
        { "conflicting-identities", RequestIdentityErrorCodes.ConflictingSubjectClaims },
        { "conflicting-in-one-identity", RequestIdentityErrorCodes.ConflictingSubjectClaims }
    };

    private static ClaimsPrincipal PrincipalFor(string cause) => cause switch
    {
        "missing" => One(new Claim("email", Sentinel)),
        "malformed" => One(new Claim("sub", Sentinel + " ")),
        "reserved" => One(new Claim("sub", "service:" + Sentinel)),
        "conflicting-identities" => new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "alice-" + Sentinel)], "bearer"),
            new ClaimsIdentity([new Claim("sub", "bob-" + Sentinel)], "cookie")
        ]),
        _ => One(new Claim("sub", "alice-" + Sentinel), new Claim("sub", "bob-" + Sentinel))
    };

    private void AssertRefusal<T>(Either<EncinaError, T> result, string code, bool invoked)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
        invoked.ShouldBeFalse();
        var record = _host.Logger.Collector.GetSnapshot().Single(r => r.Id.Id == 167);
        record.Level.ShouldBe(LogLevel.Warning);
        record.Message.ShouldContain(code);
        foreach (var logged in _host.Logger.Collector.GetSnapshot().Concat(_host.IdentityLogger.Collector.GetSnapshot()))
        {
            logged.Message.ShouldNotContain(Sentinel);
        }

        _host.Accessor.RequestContext.ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(Causes))]
    public async Task RunAsPrincipalAsync_RefusesEachCause_WithItsCode_AndNeverInvokesWork(string cause, string code)
    {
        var invoked = false;

        var result = await _host.Factory.RunAsPrincipalAsync(PrincipalFor(cause), (c, _) =>
        {
            invoked = true;
            return ScopeTestHost.Capture(c, CancellationToken.None);
        });

        AssertRefusal(result, code, invoked);
    }

    [Theory]
    [MemberData(nameof(Causes))]
    public async Task RunInboundAsync_RefusesEachCause_WithItsCode_AndNeverInvokesWork(string cause, string code)
    {
        var invoked = false;

        var result = await _host.Factory.RunInboundAsync(new InboundRequestInfo(PrincipalFor(cause)), (c, _) =>
        {
            invoked = true;
            return ScopeTestHost.Capture(c, CancellationToken.None);
        });

        AssertRefusal(result, code, invoked);
    }

    [Theory]
    [MemberData(nameof(Causes))]
    public async Task RunHostInboundAsync_RefusesEachCause_WithItsCode(string cause, string code)
    {
        var invoked = false;

        var result = await _host.Factory.RunHostInboundAsync(new InboundRequestInfo(PrincipalFor(cause)), (c, _) =>
        {
            invoked = true;
            return ScopeTestHost.Capture(c, CancellationToken.None);
        });

        AssertRefusal(result, code, invoked);
    }

    [Fact]
    public async Task SubAndOidWithDifferentValuesInOneIdentity_OpenTheScopeWithTheSubValue_OnBothPaths()
    {
        var principal = One(new Claim("sub", "pairwise"), new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "directory"));

        var principalScope = (await _host.Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture)).ShouldBeSuccess();
        var inboundScope = (await _host.Factory.RunInboundAsync(new InboundRequestInfo(principal), ScopeTestHost.Capture)).ShouldBeSuccess();

        principalScope.Issued().UserId.ShouldBe("pairwise");
        inboundScope.Issued().UserId.ShouldBe("pairwise");
    }

    [Fact]
    public async Task AnUnauthenticatedPrincipal_OpensTheAnonymousScope_OnBothPaths()
    {
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity([new Claim("email", Sentinel)]));

        var principalScope = (await _host.Factory.RunAsPrincipalAsync(unauthenticated, ScopeTestHost.Capture)).ShouldBeSuccess();
        var inboundScope = (await _host.Factory.RunInboundAsync(new InboundRequestInfo(unauthenticated), ScopeTestHost.Capture)).ShouldBeSuccess();
        var nullScope = (await _host.Factory.RunInboundAsync(new InboundRequestInfo(null), ScopeTestHost.Capture)).ShouldBeSuccess();

        principalScope.Issued().Kind.ShouldBe(IdentityKind.Anonymous);
        inboundScope.Issued().Kind.ShouldBe(IdentityKind.Anonymous);
        nullScope.Issued().Kind.ShouldBe(IdentityKind.Anonymous);
    }

    [Fact]
    public async Task ABadSubjectAndABadTenant_RefuseWithTheSubjectCode_OnBothPaths()
    {
        var principal = One(new Claim("email", "x"), new Claim("tenant_id", "t1"), new Claim("tid", "t2"));

        ShouldBeCode(await _host.Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture), RequestIdentityErrorCodes.MissingSubjectClaim);
        ShouldBeCode(
            await _host.Factory.RunInboundAsync(new InboundRequestInfo(principal), ScopeTestHost.Capture),
            RequestIdentityErrorCodes.MissingSubjectClaim);
    }

    [Fact]
    public async Task ABadSubject_WinsOverTheHolderChainOnThePrincipalPath_AndLosesToItOnTheInboundPath()
    {
        var bad = One(new Claim("email", "x"));

        await _host.InUserScope("alice", async _ =>
        {
            ShouldBeCode(await _host.Factory.RunAsPrincipalAsync(bad, ScopeTestHost.Capture), RequestIdentityErrorCodes.MissingSubjectClaim);
            ShouldBeCode(await _host.Factory.RunInboundAsync(new InboundRequestInfo(bad), ScopeTestHost.Capture), RequestIdentityErrorCodes.ScopeConflict);
            return 0;
        });
    }

    [Fact]
    public async Task ACancelledToken_WinsOverABadSubject_OnBothPaths()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var bad = One(new Claim("email", "x"));

        ShouldBeCode(await _host.Factory.RunAsPrincipalAsync(bad, ScopeTestHost.Capture, cancellationToken: cts.Token), EncinaErrorCodes.RequestCancelled);
        ShouldBeCode(
            await _host.Factory.RunInboundAsync(new InboundRequestInfo(bad), ScopeTestHost.Capture, cts.Token),
            EncinaErrorCodes.RequestCancelled);
        _host.EventIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABlankSubWithAValidOid_OpensTheScopeWithTheOidValue()
    {
        var principal = One(new Claim("sub", " "), new Claim(RequestIdentityOptions.ObjectIdentifierClaimType, "directory"));

        var scope = (await _host.Factory.RunAsPrincipalAsync(principal, ScopeTestHost.Capture)).ShouldBeSuccess();

        scope.Issued().UserId.ShouldBe("directory");
    }

    [Fact]
    public async Task AnUnsupportedAccessor_WinsOverABadSubject()
    {
        var factory = new RequestContextScopeFactory(
            Substitute.For<IRequestContextAccessor>(), _host.Catalog, _host.IdentityFactory,
            Microsoft.Extensions.Options.Options.Create(new RequestIdentityOptions()), _host.Time, _host.Logger);

        ShouldBeCode(await factory.RunAsPrincipalAsync(One(new Claim("email", "x")), ScopeTestHost.Capture), RequestIdentityErrorCodes.UnsupportedAccessor);
    }

    [Theory]
    [InlineData(nameof(RequestIdentityErrors.MissingSubjectClaim), RequestIdentityErrorCodes.MissingSubjectClaim)]
    [InlineData(nameof(RequestIdentityErrors.MalformedSubjectClaim), RequestIdentityErrorCodes.MalformedSubjectClaim)]
    [InlineData(nameof(RequestIdentityErrors.ReservedSubjectClaim), RequestIdentityErrorCodes.ReservedSubjectClaim)]
    [InlineData(nameof(RequestIdentityErrors.ConflictingSubjectClaims), RequestIdentityErrorCodes.ConflictingSubjectClaims)]
    public void TheSubjectErrors_HaveAFixedMessage_AndACodeThatIsNeitherNotFoundNorConflict(string factoryName, string code)
    {
        var error = (EncinaError)typeof(RequestIdentityErrors).GetMethod(factoryName)!.Invoke(null, null)!;

        error.GetCode().IfNone("none").ShouldBe(code);
        error.Message.ShouldNotBeNullOrWhiteSpace();
        code.ShouldStartWith("encina.identity.");
        foreach (var suffix in new[] { ".missing", ".duplicate", ".conflict", ".already_exists" })
        {
            code.ShouldNotEndWith(suffix);
        }
    }

    private static void ShouldBeCode<T>(Either<EncinaError, T> result, string code)
    {
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(error => error.GetCode().IfNone("none").ShouldBe(code));
    }
}
