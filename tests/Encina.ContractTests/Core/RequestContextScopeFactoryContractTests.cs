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

    [Fact]
    public async Task RunAsPrincipalAsync_RunInboundAsync_AndRunRestoredAsync_HonourTheConfiguredClaimMap()
    {
        var principal = Principal(new Claim("oid", "user-1"), new Claim("org", "tenant-1"), new Claim("sub", "ignored"));

        var asPrincipal = (await Scopes.RunAsPrincipalAsync(principal, Capture)).ShouldBeSuccess();
        var inbound = (await Scopes.RunInboundAsync(new InboundRequestInfo(principal), Capture)).ShouldBeSuccess();
        var restored = (await Scopes.RunRestoredAsync(
            new PersistedRequestIdentity(IdentityKind.User, "user-1", "tenant-1", "corr", null), PersistedIdentitySource.Internal, Capture)).ShouldBeSuccess();

        asPrincipal.UserId.ShouldBe("user-1");
        asPrincipal.TenantId.ShouldBe("tenant-1");
        inbound.UserId.ShouldBe("user-1");
        restored.UserId.ShouldBe("user-1");
        restored.Identity.HasClaim("oid", "user-1").ShouldBeTrue();
    }

    [Fact]
    public async Task EveryScope_IsInvalidatedWhenWorkCompletes()
    {
        var service = (await Scopes.RunAsServiceAsync(Job, Capture)).ShouldBeSuccess();
        var principal = (await Scopes.RunAsPrincipalAsync(Principal(new Claim("oid", "u")), Capture)).ShouldBeSuccess();

        service.Identity.Issuer!.IsLive.ShouldBeFalse();
        principal.Identity.Issuer!.IsLive.ShouldBeFalse();
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

        unknown.IsLeft.ShouldBeTrue();
        reserved.IsLeft.ShouldBeTrue();
        invalid.IsLeft.ShouldBeTrue();
        nested.IsLeft.ShouldBeTrue();
        invoked.ShouldBe(0);
    }
}
