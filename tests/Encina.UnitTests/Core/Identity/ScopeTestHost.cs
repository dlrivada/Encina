using System.Security.Claims;
using Encina.Testing.Identity;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Core.Identity;

/// <summary>
/// Builds a <see cref="RequestContextScopeFactory"/> over the default accessor, a catalog with the
/// declared test services, a <see cref="FakeTimeProvider"/> and a <see cref="FakeLogger{T}"/>.
/// </summary>
internal sealed class ScopeTestHost
{
    /// <summary>The declared test job: role <c>job</c>, permission <c>jobs:run</c>.</summary>
    public const string Job = "test-job";

    /// <summary>A second declared test service.</summary>
    public const string OtherJob = "other-job";

    /// <summary>The declared built-in test identity.</summary>
    public const string BuiltIn = "encina.test.seeding";

    public ScopeTestHost(RequestIdentityOptions? options = null)
    {
        Options = options ?? new RequestIdentityOptions();
        var catalogOptions = new ServiceIdentityCatalogOptions();
        catalogOptions.Declare(new ServiceIdentityBuilder().WithRoles("job").WithPermissions("jobs:run").Build(Job, isBuiltIn: false));
        catalogOptions.Declare(new ServiceIdentityBuilder().WithRoles("other").Build(OtherJob, isBuiltIn: false));
        catalogOptions.Declare(new ServiceIdentityBuilder().WithPermissions("policies:seed").Build(BuiltIn, isBuiltIn: true));
        Catalog = new ServiceIdentityCatalog(Microsoft.Extensions.Options.Options.Create(catalogOptions));
        IdentityFactory = new ClaimsRequestIdentityFactory(Microsoft.Extensions.Options.Options.Create(Options), IdentityLogger);
        Factory = new RequestContextScopeFactory(
            Accessor, Catalog, IdentityFactory, Microsoft.Extensions.Options.Options.Create(Options), Time, Logger);
    }

    public RequestContextAccessor Accessor { get; } = new();

    public RequestIdentityOptions Options { get; }

    public ServiceIdentityCatalog Catalog { get; }

    public ClaimsRequestIdentityFactory IdentityFactory { get; }

    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 10, 6, 9, 0, 0, TimeSpan.Zero));

    public FakeLogger<RequestContextScopeFactory> Logger { get; } = new();

    public FakeLogger<ClaimsRequestIdentityFactory> IdentityLogger { get; } = new();

    public RequestContextScopeFactory Factory { get; }

    /// <summary>The EventIds logged by the scope factory, in order.</summary>
    public IReadOnlyList<int> EventIds => [.. Logger.Collector.GetSnapshot().Select(static record => record.Id.Id)];

    /// <summary>A principal the default claim map maps to a user.</summary>
    public static ClaimsPrincipal UserPrincipal(string userId, string? tenant = null, params string[] roles)
    {
        IEnumerable<Claim> extra = tenant is null ? [] : [new Claim("tenant_id", tenant)];
        return TestIdentity.Principal(userId, roles, claims: extra);
    }

    /// <summary>Work that returns the context it ran with.</summary>
    public static Task<Either<EncinaError, IRequestContext>> Capture(IRequestContext context, CancellationToken cancellationToken) =>
        Task.FromResult(Right<EncinaError, IRequestContext>(context));

    /// <summary>Work that returns the ambient context read through <paramref name="accessor"/>.</summary>
    public static Func<IRequestContext, CancellationToken, Task<Either<EncinaError, IRequestContext?>>> ReadAmbient(IRequestContextAccessor accessor) =>
        (_, _) => Task.FromResult(Right<EncinaError, IRequestContext?>(accessor.RequestContext));

    /// <summary>Opens a user scope through the factory and returns what <paramref name="inside"/> returns.</summary>
    public async Task<T> InUserScope<T>(string userId, Func<IRequestContext, Task<T>> inside)
    {
        var result = await Factory.RunAsPrincipalAsync(
            UserPrincipal(userId),
            async (context, _) => Right<EncinaError, T>(await inside(context)));
        return result.Match(Right: value => value, Left: error => throw new InvalidOperationException(error.GetCode().IfNone("unknown")));
    }

    /// <summary>Opens an anonymous inbound scope through the factory and returns what <paramref name="inside"/> returns.</summary>
    public async Task<T> InInboundScope<T>(ClaimsPrincipal? principal, Func<IRequestContext, Task<T>> inside)
    {
        var result = await Factory.RunHostInboundAsync(
            new InboundRequestInfo(principal, "inbound-correlation"),
            async (context, _) => Right<EncinaError, T>(await inside(context)));
        return result.Match(Right: value => value, Left: error => throw new InvalidOperationException(error.GetCode().IfNone("unknown")));
    }
}
