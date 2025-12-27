using Encina.Messaging.ContentRouter;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.ContentRouter;

public sealed class ContentRouterBuilderTests
{
    [Fact]
    public void Create_ReturnsNewBuilder()
    {
        // Act
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithoutResult_ReturnsBuilderForUnit()
    {
        // Act
        var builder = ContentRouterBuilder.Create<TestOrder>();

        // Assert
        builder.ShouldNotBeNull();
    }

    [Fact]
    public void When_WithNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.When(null!));
    }

    [Fact]
    public void When_WithNameAndNullCondition_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.When("Route", null!));
    }

    [Fact]
    public void When_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When(null!, o => true));
    }

    [Fact]
    public void When_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.When("", o => true));
    }

    [Fact]
    public void RouteTo_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.RouteTo((Func<TestOrder, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
    }

    [Fact]
    public void Default_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            builder.Default((Func<TestOrder, CancellationToken, ValueTask<Either<EncinaError, string>>>)null!));
    }

    [Fact]
    public void Build_WithNoRoutes_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithOnlyDefaultRoute_ReturnsDefinition()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .Default(o => Right<EncinaError, string>("default"))
            .Build();

        // Assert
        definition.ShouldNotBeNull();
        definition.HasDefaultRoute.ShouldBeTrue();
        definition.RouteCount.ShouldBe(0);
    }

    [Fact]
    public void Build_WithRoutes_ReturnsDefinitionWithRoutes()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .When(o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("medium"))
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(2);
        definition.HasDefaultRoute.ShouldBeFalse();
    }

    [Fact]
    public void Build_WithPriorities_SortsRoutesByPriority()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("Low", o => o.Total > 0)
            .WithPriority(10)
            .RouteTo(o => Right<EncinaError, string>("low"))
            .When("High", o => o.Total > 0)
            .WithPriority(1)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .When("Medium", o => o.Total > 0)
            .WithPriority(5)
            .RouteTo(o => Right<EncinaError, string>("medium"))
            .Build();

        // Assert
        definition.Routes[0].Name.ShouldBe("High");
        definition.Routes[1].Name.ShouldBe("Medium");
        definition.Routes[2].Name.ShouldBe("Low");
    }

    [Fact]
    public void When_WithMetadata_IncludesMetadataInRoute()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("TestRoute", o => o.Total > 0)
            .WithMetadata("key1", "value1")
            .WithMetadata("key2", 42)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        // Assert
        var route = definition.Routes[0];
        route.Metadata.ShouldNotBeNull();
        route.Metadata["key1"].ShouldBe("value1");
        route.Metadata["key2"].ShouldBe(42);
    }

    [Fact]
    public void RouteTo_WithSyncHandler_WrapsCorrectly()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>($"Total: {o.Total}"))
            .Build();

        // Assert
        definition.Routes.Count.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithSimpleSyncHandler_WrapsCorrectly()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => $"Total: {o.Total}")
            .Build();

        // Assert
        definition.Routes.Count.ShouldBe(1);
    }

    [Fact]
    public void RouteTo_WithSimpleAsyncHandler_WrapsCorrectly()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(1, ct);
                return $"Total: {o.Total}";
            })
            .Build();

        // Assert
        definition.Routes.Count.ShouldBe(1);
    }

    [Fact]
    public void DefaultResult_SetsDefaultRouteWithValue()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .DefaultResult("standard")
            .Build();

        // Assert
        definition.HasDefaultRoute.ShouldBeTrue();
        definition.DefaultRoute!.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Default_WithSyncHandler_WrapsCorrectly()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .Default(o => Right<EncinaError, string>("default"))
            .Build();

        // Assert
        definition.HasDefaultRoute.ShouldBeTrue();
    }

    [Fact]
    public void When_GeneratesRouteNames()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("one"))
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("two"))
            .Build();

        // Assert
        definition.Routes[0].Name.ShouldBe("Route_1");
        definition.Routes[1].Name.ShouldBe("Route_2");
    }

    [Fact]
    public void WithMetadata_WithNullKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.WithMetadata(null!, "value"));
    }

    [Fact]
    public void WithMetadata_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentException>(() => builder.WithMetadata("", "value"));
    }

    [Fact]
    public void WithMetadata_WithNullValue_ThrowsArgumentNullException()
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0);

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.WithMetadata("key", null!));
    }

    public class TestOrder
    {
        public decimal Total { get; set; }
    }
}
