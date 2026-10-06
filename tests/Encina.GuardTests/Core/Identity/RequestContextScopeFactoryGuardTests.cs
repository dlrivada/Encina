using System.Security.Claims;
using Encina.Testing.Identity;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Identity;

/// <summary>
/// Guard clauses of the scope API, the service identity catalog and their registration.
/// </summary>
public sealed class RequestContextScopeFactoryGuardTests
{
    private static readonly IOptions<RequestIdentityOptions> IdentityOptions = Options.Create(new RequestIdentityOptions());

    private static readonly Func<IRequestContext, CancellationToken, Task<Either<EncinaError, int>>> Work =
        (_, _) => Task.FromResult(Right<EncinaError, int>(1));

    private static readonly Func<IRequestContext, CancellationToken, Task> PlainWork = (_, _) => Task.CompletedTask;

    private static readonly PersistedRequestIdentity Persisted = new(IdentityKind.Anonymous, null, null, "corr", null);

    private static RequestContextScopeFactory CreateFactory()
    {
        var catalog = new ServiceIdentityCatalog(Options.Create(new ServiceIdentityCatalogOptions()));
        return new RequestContextScopeFactory(
            new RequestContextAccessor(), catalog, new ClaimsRequestIdentityFactory(IdentityOptions), IdentityOptions, TimeProvider.System);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        var accessor = new RequestContextAccessor();
        var catalog = new ServiceIdentityCatalog(Options.Create(new ServiceIdentityCatalogOptions()));
        var identityFactory = new ClaimsRequestIdentityFactory(IdentityOptions);

        Should.Throw<ArgumentNullException>(() => new RequestContextScopeFactory(null!, catalog, identityFactory, IdentityOptions, TimeProvider.System));
        Should.Throw<ArgumentNullException>(() => new RequestContextScopeFactory(accessor, null!, identityFactory, IdentityOptions, TimeProvider.System));
        Should.Throw<ArgumentNullException>(() => new RequestContextScopeFactory(accessor, catalog, null!, IdentityOptions, TimeProvider.System));
        Should.Throw<ArgumentNullException>(() => new RequestContextScopeFactory(accessor, catalog, identityFactory, null!, TimeProvider.System));
        Should.Throw<ArgumentNullException>(() => new RequestContextScopeFactory(accessor, catalog, identityFactory, IdentityOptions, null!));
    }

    [Fact]
    public async Task Members_NullArguments_Throw()
    {
        var factory = CreateFactory();

        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsServiceAsync(null!, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsServiceAsync<int>("job", null!));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsBuiltInAsync(null!, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsBuiltInAsync<int>("encina.x", null!));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsPrincipalAsync(null!, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunAsPrincipalAsync<int>(new ClaimsPrincipal(), null!));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunInboundAsync(null!, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunInboundAsync<int>(new InboundRequestInfo(null), null!));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunHostInboundAsync(null!, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunRestoredAsync(null!, PersistedIdentitySource.Internal, Work));
        await Should.ThrowAsync<ArgumentNullException>(() => factory.RunRestoredAsync<int>(Persisted, PersistedIdentitySource.Internal, null!));
    }

    [Fact]
    public void Extensions_NullArguments_Throw()
    {
        IRequestContextScopeFactory factory = CreateFactory();

        Should.Throw<ArgumentNullException>(() => RequestContextScopeFactoryExtensions.RunAsServiceAsync(null!, "job", PlainWork));
        Should.Throw<ArgumentNullException>(() => RequestContextScopeFactoryExtensions.RunAsPrincipalAsync(null!, new ClaimsPrincipal(), PlainWork));
        Should.Throw<ArgumentNullException>(() => RequestContextScopeFactoryExtensions.RunInboundAsync(null!, new InboundRequestInfo(null), PlainWork));
        Should.Throw<ArgumentNullException>(() => RequestContextScopeFactoryExtensions.RunRestoredAsync(null!, Persisted, PersistedIdentitySource.Internal, PlainWork));
        Should.Throw<ArgumentNullException>(() => factory.RunAsServiceAsync("job", (Func<IRequestContext, CancellationToken, Task>)null!));
    }

    [Fact]
    public void CatalogBuilderAndValidators_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityCatalog(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityCatalogOptionsValidator(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityCatalogOptionsValidator(IdentityOptions).Validate(null, null!));
        Should.Throw<ArgumentNullException>(() => new RequestIdentityOptionsValidator().Validate(null, null!));
        Should.Throw<ArgumentNullException>(() => new RequestContextAccessorRequirementValidator(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityCatalogOptions().Declare(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityBuilder().WithPermissions(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceIdentityBuilder().WithClaim(null!, "v"));
        Should.Throw<ArgumentNullException>(() => RequestIdentity.ForService(null!));
        Should.Throw<ArgumentNullException>(() => new IdentityIssuer().Bind(null!));
        Should.Throw<ArgumentNullException>(() => InboundRequestInfo.ForCircuit(null, null!));
    }

    [Fact]
    public void Registration_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaServiceIdentity("job"));
        Should.Throw<ArgumentException>(() => new ServiceCollection().AddEncinaServiceIdentity(null!));
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddBuiltInServiceIdentity("encina.x"));
        Should.Throw<ArgumentNullException>(() => ((IServiceCollection)null!).AddEncinaRequestIdentity());
    }

    [Fact]
    public void TestIdentityBuilders_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => TestIdentity.Principal(null!));
        Should.Throw<ArgumentException>(() => TestIdentity.Service(null!));
    }
}
