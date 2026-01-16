using Encina.Messaging.ContentRouter;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.ContentRouter;

/// <summary>
/// Unit tests for <see cref="ContentRouterBuilder"/> and related types.
/// </summary>
public sealed class ContentRouterBuilderTests
{
    #region ContentRouterBuilder Static Factory Tests

    [Fact]
    public void Create_WithResult_ReturnsBuilder()
    {
        // Act
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<ContentRouterBuilder<TestMessage, string>>();
    }

    [Fact]
    public void Create_WithoutResult_ReturnsBuilderWithUnit()
    {
        // Act
        var builder = ContentRouterBuilder.Create<TestMessage>();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeOfType<ContentRouterBuilder<TestMessage, Unit>>();
    }

    #endregion

    #region ContentRouterBuilder<TMessage, TResult> Tests

    [Fact]
    public void When_WithCondition_ReturnsContentRouteBuilder()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var routeBuilder = builder.When(m => m.Priority > 5);

        // Assert
        routeBuilder.ShouldNotBeNull();
        routeBuilder.ShouldBeOfType<ContentRouteBuilder<TestMessage, string>>();
    }

    [Fact]
    public void When_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.When(null!));
    }

    [Fact]
    public void When_WithNameAndCondition_ReturnsContentRouteBuilder()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var routeBuilder = builder.When("HighPriority", m => m.Priority > 5);

        // Assert
        routeBuilder.ShouldNotBeNull();
    }

    [Fact]
    public void When_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When(null!, m => true));
    }

    [Fact]
    public void When_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When(string.Empty, m => true));
    }

    [Fact]
    public void When_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When("  ", m => true));
    }

    [Fact]
    public void Default_WithAsyncHandler_SetsDefaultRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var result = builder.Default(async (message, ct) =>
        {
            await Task.Delay(1, ct);
            return Right<EncinaError, string>("default");
        });

        // Assert
        result.ShouldBeSameAs(builder);
        var definition = builder.Build();
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    [Fact]
    public void Default_WithNullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.Default((Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
    }

    [Fact]
    public void Default_WithSyncHandler_SetsDefaultRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var result = builder.Default(message => Right<EncinaError, string>("sync default"));

        // Assert
        result.ShouldBeSameAs(builder);
        var definition = builder.Build();
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    [Fact]
    public void Default_WithNullSyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.Default((Func<TestMessage, Either<EncinaError, string>>)null!));
    }

    [Fact]
    public void DefaultResult_SetsDefaultRouteWithValue()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        const string defaultValue = "default-result";

        // Act
        var result = builder.DefaultResult(defaultValue);

        // Assert
        result.ShouldBeSameAs(builder);
        var definition = builder.Build();
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithNoRoutes_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build())
            .Message.ShouldContain("At least one route");
    }

    [Fact]
    public void Build_WithOnlyDefaultRoute_Succeeds()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        builder.DefaultResult("default");

        // Act
        var definition = builder.Build();

        // Assert
        definition.ShouldNotBeNull();
        definition.RouteCount.ShouldBe(0);
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    [Fact]
    public void Build_WithRoutes_ReturnsDefinition()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        builder
            .When(m => m.Priority > 5)
            .RouteTo(m => "high")
            .When(m => m.Priority <= 5)
            .RouteTo(m => "low");

        // Act
        var definition = builder.Build();

        // Assert
        definition.RouteCount.ShouldBe(2);
        definition.HasDefaultRoute.ShouldBeFalse();
    }

    [Fact]
    public void Build_SortsByPriority()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        builder
            .When(m => m.Priority > 8).WithPriority(2).RouteTo(m => "low-priority-route")
            .When(m => m.Priority > 5).WithPriority(1).RouteTo(m => "high-priority-route");

        // Act
        var definition = builder.Build();

        // Assert
        definition.Routes[0].Priority.ShouldBe(1);
        definition.Routes[1].Priority.ShouldBe(2);
    }

    #endregion

    #region ContentRouteBuilder<TMessage, TResult> Tests

    [Fact]
    public void WithPriority_SetsPriority()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When("TestRoute", m => true)
            .WithPriority(5)
            .RouteTo(m => "result")
            .Build();

        // Assert
        definition.Routes[0].Priority.ShouldBe(5);
    }

    [Fact]
    public void WithMetadata_AddsMetadata()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When("TestRoute", m => true)
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .RouteTo(m => "result")
            .Build();

        // Assert
        var route = definition.Routes[0];
        route.Metadata.ShouldNotBeNull();
        route.Metadata["key1"].ShouldBe("value1");
        route.Metadata["key2"].ShouldBe(42);
    }

    [Fact]
    public void WithMetadata_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentException>(() => routeBuilder.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithMetadata_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentException>(() => routeBuilder.WithMetadata(string.Empty, "value"));
    }

    [Fact]
    public void WithMetadata_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => routeBuilder.WithMetadata("key", null!));
    }

    [Fact]
    public void RouteTo_WithAsyncEitherHandler_AddsRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When(m => true)
            .RouteTo(async (message, ct) =>
            {
                await Task.Delay(1, ct);
                return Right<EncinaError, string>("result");
            })
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithNullAsyncEitherHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestMessage, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
    }

    [Fact]
    public void RouteTo_WithSyncEitherHandler_AddsRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When(m => true)
            .RouteTo(message => Right<EncinaError, string>("result"))
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithNullSyncEitherHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestMessage, Either<EncinaError, string>>)null!));
    }

    [Fact]
    public void RouteTo_WithAsyncHandler_AddsRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When(m => true)
            .RouteTo(async (message, ct) =>
            {
                await Task.Delay(1, ct);
                return "async result";
            })
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithNullAsyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestMessage, CancellationToken, ValueTask<string>>)null!));
    }

    [Fact]
    public void RouteTo_WithSyncHandler_AddsRoute()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When(m => true)
            .RouteTo(message => "sync result")
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithNullSyncHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var routeBuilder = builder.When(m => true);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            routeBuilder.RouteTo((Func<TestMessage, string>)null!));
    }

    [Fact]
    public void RouteName_WhenNotProvided_IsGenerated()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When(m => true).RouteTo(m => "result1")
            .When(m => false).RouteTo(m => "result2")
            .Build();

        // Assert
        definition.Routes[0].Name.ShouldBe("Route_1");
        definition.Routes[1].Name.ShouldBe("Route_2");
    }

    [Fact]
    public void RouteName_WhenProvided_IsUsed()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();

        // Act
        var definition = builder
            .When("CustomRoute", m => true).RouteTo(m => "result")
            .Build();

        // Assert
        definition.Routes[0].Name.ShouldBe("CustomRoute");
    }

    #endregion

    #region BuiltContentRouterDefinition Tests

    [Fact]
    public void HasDefaultRoute_WhenNoDefault_ReturnsFalse()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var definition = builder.When(m => true).RouteTo(m => "result").Build();

        // Act & Assert
        definition.HasDefaultRoute.ShouldBeFalse();
    }

    [Fact]
    public void HasDefaultRoute_WhenDefault_ReturnsTrue()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestMessage, string>();
        var definition = builder.DefaultResult("default").Build();

        // Act & Assert
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    #endregion

    private sealed class TestMessage
    {
        public int Priority { get; set; }
    }
}
