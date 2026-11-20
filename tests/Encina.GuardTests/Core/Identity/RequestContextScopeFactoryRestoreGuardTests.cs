using LanguageExt;
using Microsoft.Extensions.Options;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core.Identity;

/// <summary>
/// Guard tests for <c>RunRestoredAsync</c>: null arguments throw and an undefined source is refused
/// without invoking the work.
/// </summary>
public sealed class RequestContextScopeFactoryRestoreGuardTests
{
    private static readonly IOptions<RequestIdentityOptions> IdentityOptions = Options.Create(new RequestIdentityOptions());

    private static readonly PersistedRequestIdentity Anonymous = new(IdentityKind.Anonymous, null, null, "corr", null);

    private static RequestContextScopeFactory CreateFactory() =>
        new(
            new RequestContextAccessor(),
            new ServiceIdentityCatalog(Options.Create(new ServiceIdentityCatalogOptions())),
            new ClaimsRequestIdentityFactory(IdentityOptions),
            IdentityOptions,
            TimeProvider.System);

    [Fact]
    public async Task RunRestoredAsync_NullPersisted_ThrowsArgumentNullException()
    {
        var factory = CreateFactory();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            factory.RunRestoredAsync<int>(null!, PersistedIdentitySource.Internal, (_, _) => Task.FromResult(Right<EncinaError, int>(1))));

        ex.ParamName.ShouldBe("persisted");
    }

    [Fact]
    public async Task RunRestoredAsync_NullWork_ThrowsArgumentNullException()
    {
        var factory = CreateFactory();

        var ex = await Should.ThrowAsync<ArgumentNullException>(() =>
            factory.RunRestoredAsync<int>(Anonymous, PersistedIdentitySource.Internal, null!));

        ex.ParamName.ShouldBe("work");
    }

    [Fact]
    public async Task RunRestoredAsync_UndefinedSource_ReturnsLeftWithoutRunningWork()
    {
        var factory = CreateFactory();
        var ran = false;

        var result = await factory.RunRestoredAsync<int>(
            Anonymous,
            (PersistedIdentitySource)999,
            (_, _) =>
            {
                ran = true;
                return Task.FromResult(Right<EncinaError, int>(1));
            });

        result.IsLeft.ShouldBeTrue();
        ran.ShouldBeFalse();
    }

    [Fact]
    public async Task RunRestoredAsync_ValidAnonymousInternal_RunsTheWork()
    {
        var factory = CreateFactory();

        var result = await factory.RunRestoredAsync<int>(
            Anonymous,
            PersistedIdentitySource.Internal,
            (_, _) => Task.FromResult(Right<EncinaError, int>(7)));

        result.Match(Right: value => value, Left: _ => -1).ShouldBe(7);
    }
}
