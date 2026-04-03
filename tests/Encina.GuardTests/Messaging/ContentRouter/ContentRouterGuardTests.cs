using Encina.Messaging.ContentRouter;
using FluentAssertions;
using LanguageExt;
using ContentRouterClass = Encina.Messaging.ContentRouter.ContentRouter;

namespace Encina.GuardTests.Messaging.ContentRouter;

/// <summary>
/// Guard clause tests for ContentRouter, ContentRouterBuilder, ContentRouteBuilder, and RouteDefinition.
/// </summary>
public class ContentRouterGuardTests
{
    #region ContentRouter Constructor

    [Fact]
    public void ContentRouter_NullOptions_ThrowsArgumentNullException()
    {
        var logger = NullLogger<ContentRouterClass>.Instance;

        var act = () => new ContentRouterClass(null!, logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void ContentRouter_NullLogger_ThrowsArgumentNullException()
    {
        var options = new ContentRouterOptions();

        var act = () => new ContentRouterClass(options, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region ContentRouter.RouteAsync

    [Fact]
    public async Task RouteAsync_NullDefinition_ThrowsArgumentNullException()
    {
        var router = CreateRouter();

        var act = async () => await router.RouteAsync<TestMessage, string>(null!, new TestMessage());

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("definition");
    }

    [Fact]
    public async Task RouteAsync_NullMessage_ThrowsArgumentNullException()
    {
        var router = CreateRouter();
        var definition = ContentRouterBuilder.Create<TestMessage, string>()
            .When(_ => true)
            .RouteTo(m => Either<EncinaError, string>.Right("ok"))
            .Build();

        var act = async () => await router.RouteAsync<TestMessage, string>(definition, null!);

        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("message");
    }

    #endregion

    #region ContentRouterBuilder<TMessage, TResult>.When

    [Fact]
    public void When_NullCondition_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.When((Func<TestMessage, bool>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
    }

    [Fact]
    public void When_Named_NullName_ThrowsArgumentException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.When(null!, _ => true);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void When_Named_EmptyName_ThrowsArgumentException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.When("", _ => true);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void When_Named_WhitespaceName_ThrowsArgumentException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.When("   ", _ => true);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void When_Named_NullCondition_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.When("route1", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
    }

    #endregion

    #region ContentRouterBuilder<TMessage, TResult>.Default

    [Fact]
    public void Default_NullAsyncHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.Default(
            (Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void Default_NullSyncHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.Default((Func<TestMessage, Either<EncinaError, string>>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    #endregion

    #region ContentRouterBuilder<TMessage, TResult>.Build

    [Fact]
    public void Build_NoRoutes_ThrowsInvalidOperationException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region ContentRouteBuilder<TMessage, TResult>.WithMetadata

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.WithMetadata(null!, "value");

        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.WithMetadata("", "value");

        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void WithMetadata_NullValue_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.WithMetadata("key", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    #endregion

    #region ContentRouteBuilder<TMessage, TResult>.RouteTo

    [Fact]
    public void RouteTo_NullAsyncEitherHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.RouteTo(
            (Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void RouteTo_NullSyncEitherHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.RouteTo((Func<TestMessage, Either<EncinaError, string>>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void RouteTo_NullAsyncHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.RouteTo(
            (Func<TestMessage, CancellationToken, ValueTask<string>>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    [Fact]
    public void RouteTo_NullSyncHandler_ThrowsArgumentNullException()
    {
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(_ => true);

        var act = () => routeBuilder.RouteTo((Func<TestMessage, string>)null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    #endregion

    #region RouteDefinition Constructor

    [Fact]
    public void RouteDefinition_NullName_ThrowsArgumentException()
    {
        var act = () => new RouteDefinition<TestMessage, string>(
            null!,
            _ => true,
            (_, _) => ValueTask.FromResult(Either<EncinaError, string>.Right("ok")));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void RouteDefinition_EmptyName_ThrowsArgumentException()
    {
        var act = () => new RouteDefinition<TestMessage, string>(
            "",
            _ => true,
            (_, _) => ValueTask.FromResult(Either<EncinaError, string>.Right("ok")));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void RouteDefinition_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => new RouteDefinition<TestMessage, string>(
            "   ",
            _ => true,
            (_, _) => ValueTask.FromResult(Either<EncinaError, string>.Right("ok")));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void RouteDefinition_NullCondition_ThrowsArgumentNullException()
    {
        var act = () => new RouteDefinition<TestMessage, string>(
            "route1",
            null!,
            (_, _) => ValueTask.FromResult(Either<EncinaError, string>.Right("ok")));

        act.Should().Throw<ArgumentNullException>().WithParameterName("condition");
    }

    [Fact]
    public void RouteDefinition_NullHandler_ThrowsArgumentNullException()
    {
        var act = () => new RouteDefinition<TestMessage, string>(
            "route1",
            _ => true,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("handler");
    }

    #endregion

    #region Helpers

    private static ContentRouterClass CreateRouter()
    {
        return new ContentRouterClass(
            new ContentRouterOptions(),
            NullLogger<ContentRouterClass>.Instance);
    }

    public sealed class TestMessage
    {
        public string Value { get; set; } = "test";
    }

    #endregion
}
