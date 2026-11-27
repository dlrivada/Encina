using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="AmbientRequestContext"/> and <see cref="StreamDispatcher"/>: the internal entry points
/// check no argument themselves, so these tests pin the accepted nullable inputs and the dispatcher's refusal paths.
/// </summary>
public class AmbientRequestContextAndStreamGuardTests
{
    private sealed record UnhandledStream : IStreamRequest<int>;

    private static IRequestContext Unwrap(Either<EncinaError, IRequestContext> result)
        => result.Match(Right: context => context, Left: _ => throw new InvalidOperationException("Expected a Right result."));

    #region AmbientRequestContext.Resolve

    /// <summary>
    /// With no explicit context and no ambient one, Resolve creates an anonymous context.
    /// </summary>
    [Fact]
    public void Resolve_NoExplicitNoAmbient_ReturnsAnonymousContext()
    {
        var accessor = new RequestContextAccessor();

        var result = AmbientRequestContext.Resolve(accessor, null, TimeProvider.System, NullLogger.Instance);

        result.IsRight.ShouldBeTrue();
        Unwrap(result).Identity.IsAuthenticated.ShouldBeFalse();
    }

    /// <summary>
    /// An ambient context outside a dispatch is returned as is; the time provider is not read.
    /// </summary>
    [Fact]
    public void Resolve_AmbientOutsideDispatch_ReturnsAmbientWithoutTimeProvider()
    {
        var accessor = new RequestContextAccessor();
        var ambient = RequestContext.CreateForTest();
        accessor.RequestContext = ambient;

        var result = AmbientRequestContext.Resolve(accessor, null, null!, NullLogger.Instance);

        result.IsRight.ShouldBeTrue();
        Unwrap(result).CorrelationId.ShouldBe(ambient.CorrelationId);
    }

    /// <summary>
    /// An explicit anonymous context over an anonymous ambient one is accepted and copied.
    /// </summary>
    [Fact]
    public void Resolve_ExplicitAnonymousContext_IsAccepted()
    {
        var accessor = new RequestContextAccessor();
        var explicitContext = RequestContext.CreateForTest();

        var result = AmbientRequestContext.Resolve(accessor, explicitContext, TimeProvider.System, NullLogger.Instance);

        result.IsRight.ShouldBeTrue();
        Unwrap(result).CorrelationId.ShouldBe(explicitContext.CorrelationId);
    }

    #endregion

    #region StreamDispatcher.ExecuteAsync

    /// <summary>
    /// A stream request without a registered handler yields one HandlerMissing error and ends.
    /// </summary>
    [Fact]
    public async Task Stream_RequestWithoutHandler_YieldsHandlerMissingError()
    {
        using var provider = new ServiceCollection().AddEncina().BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var items = new List<Either<EncinaError, int>>();
        await foreach (var item in encina.Stream(new UnhandledStream(), RequestContext.CreateForTest(), CancellationToken.None))
        {
            items.Add(item);
        }

        items.Count.ShouldBe(1);
        items[0].IsLeft.ShouldBeTrue();
        ((EncinaError)items[0]).GetCode().IfNone(string.Empty).ShouldBe(EncinaErrorCodes.HandlerMissing);
    }

    /// <summary>
    /// A null stream request is refused before it reaches the dispatcher.
    /// </summary>
    [Fact]
    public async Task Stream_NullRequest_YieldsSingleError()
    {
        using var provider = new ServiceCollection().AddEncina().BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var items = new List<Either<EncinaError, int>>();
        await foreach (var item in encina.Stream<int>(null!, CancellationToken.None))
        {
            items.Add(item);
        }

        items.ShouldHaveSingleItem().IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// A null explicit context is refused eagerly, before the stream is enumerated.
    /// </summary>
    [Fact]
    public void Stream_NullContext_ThrowsArgumentNullException()
    {
        using var provider = new ServiceCollection().AddEncina().BuildServiceProvider();
        var encina = provider.GetRequiredService<IEncina>();

        var act = () => encina.Stream(new UnhandledStream(), null!, CancellationToken.None);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("context");
    }

    #endregion
}
