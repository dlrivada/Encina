using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for Content-Based Router components.
/// </summary>
public sealed class ContentRouterGuardsTests
{
    #region ContentRouter Guards

    [Fact]
    public void ContentRouter_Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new Messaging.ContentRouter.ContentRouter(null!, logger));
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void ContentRouter_Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ContentRouterOptions();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new Messaging.ContentRouter.ContentRouter(options, null!));
        ex.ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task ContentRouter_RouteAsync_NullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ContentRouterOptions();
        var logger = Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();
        var router = new Messaging.ContentRouter.ContentRouter(options, logger);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await router.RouteAsync<TestOrder, string>(null!, new TestOrder()));
        ex.ParamName.ShouldBe("definition");
    }

    [Fact]
    public async Task ContentRouter_RouteAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ContentRouterOptions();
        var logger = Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();
        var router = new Messaging.ContentRouter.ContentRouter(options, logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () =>
            await router.RouteAsync(definition, null!));
        ex.ParamName.ShouldBe("message");
    }

    #endregion

    #region ContentRouterBuilder Guards

    [Fact]
    public void ContentRouterBuilder_When_NullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => builder.When(null!));
        ex.ParamName.ShouldBe("condition");
    }

    [Fact]
    public void ContentRouterBuilder_When_NullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When(null!, o => true));
    }

    [Fact]
    public void ContentRouterBuilder_When_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When("", o => true));
    }

    [Fact]
    public void ContentRouterBuilder_When_WhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When("   ", o => true));
    }

    [Fact]
    public void ContentRouterBuilder_Default_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.Default((Func<TestOrder, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouterBuilder_DefaultSync_NullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            builder.Default((Func<TestOrder, Either<EncinaError, string>>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouterBuilder_Build_NoRoutes_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    #endregion

    #region ContentRouteBuilder Guards

    [Fact]
    public void ContentRouteBuilder_RouteTo_NullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestOrder, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouteBuilder_RouteTo_NullSyncEitherHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestOrder, Either<EncinaError, string>>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouteBuilder_RouteTo_NullAsyncSimpleHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestOrder, CancellationToken, ValueTask<string>>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouteBuilder_RouteTo_NullSyncSimpleHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestOrder, string>)null!));
        ex.ParamName.ShouldBe("handler");
    }

    [Fact]
    public void ContentRouteBuilder_WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => routeBuilder.WithMetadata(null!, "value"));
    }

    [Fact]
    public void ContentRouteBuilder_WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => routeBuilder.WithMetadata("", "value"));
    }

    [Fact]
    public void ContentRouteBuilder_WithMetadata_WhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => routeBuilder.WithMetadata("   ", "value"));
    }

    [Fact]
    public void ContentRouteBuilder_WithMetadata_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var routeBuilder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() => routeBuilder.WithMetadata("key", null!));
        ex.ParamName.ShouldBe("value");
    }

    #endregion

    #region RouteDefinition Guards

    [Fact]
    public void RouteDefinition_Constructor_NullName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestOrder, string>(
                null!,
                o => true,
                (o, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"))));
    }

    [Fact]
    public void RouteDefinition_Constructor_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestOrder, string>(
                "",
                o => true,
                (o, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"))));
    }

    [Fact]
    public void RouteDefinition_Constructor_WhitespaceName_ThrowsArgumentException()
    {
        // Act & Assert
        Should.Throw<ArgumentException>(() =>
            new RouteDefinition<TestOrder, string>(
                "   ",
                o => true,
                (o, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"))));
    }

    [Fact]
    public void RouteDefinition_Constructor_NullCondition_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RouteDefinition<TestOrder, string>(
                "Test",
                null!,
                (o, ct) => ValueTask.FromResult(Right<EncinaError, string>("result"))));
        ex.ParamName.ShouldBe("condition");
    }

    [Fact]
    public void RouteDefinition_Constructor_NullHandler_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Should.Throw<ArgumentNullException>(() =>
            new RouteDefinition<TestOrder, string>(
                "Test",
                o => true,
                null!));
        ex.ParamName.ShouldBe("handler");
    }

    #endregion

    public class TestOrder
    {
        public decimal Total { get; set; }
    }
}
