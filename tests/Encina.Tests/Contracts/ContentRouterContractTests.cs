using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Contracts;

/// <summary>
/// Contract tests verifying that the Content-Based Router implementation
/// follows the expected interface contract and behavior.
/// </summary>
public sealed class ContentRouterContractTests
{
    private readonly ContentRouterOptions _options = new();
    private readonly ILogger<Messaging.ContentRouter.ContentRouter> _logger =
        Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();

    #region IContentRouter Contract

    /// <summary>
    /// Contract: RouteAsync with valid inputs must return either success or error, never throw.
    /// </summary>
    [Fact]
    public async Task RouteAsync_WithValidInputs_NeverThrows()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert - should complete without exception
        (result.IsLeft || result.IsRight).ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RouteAsync must return Right when a route matches and succeeds.
    /// </summary>
    [Fact]
    public async Task RouteAsync_MatchingRoute_ReturnsRight()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("matched"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RouteAsync must return Left when no route matches and ThrowOnNoMatch is true.
    /// </summary>
    [Fact]
    public async Task RouteAsync_NoMatch_ReturnsLeft()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("never"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsLeft.ShouldBeTrue();
    }

    /// <summary>
    /// Contract: RouteAsync with Unit result type must work correctly.
    /// </summary>
    [Fact]
    public async Task RouteAsync_UnitResult_WorksCorrectly()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var executed = false;
        var definition = ContentRouterBuilder.Create<TestOrder>()
            .When(o => o.Total > 0)
            .RouteTo((o, ct) =>
            {
                executed = true;
                return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
            })
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    #endregion

    #region ContentRouterResult Contract

    /// <summary>
    /// Contract: ContentRouterResult must have non-null RouteResults collection.
    /// </summary>
    [Fact]
    public async Task ContentRouterResult_RouteResults_NeverNull()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("never"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.Match(Right: r => r, Left: _ => null!);
        routerResult.RouteResults.ShouldNotBeNull();
    }

    /// <summary>
    /// Contract: ContentRouterResult.MatchedRouteCount must be >= 0.
    /// </summary>
    [Fact]
    public async Task ContentRouterResult_MatchedRouteCount_NonNegative()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("never"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.Match(Right: r => r, Left: _ => null!);
        routerResult.MatchedRouteCount.ShouldBeGreaterThanOrEqualTo(0);
    }

    /// <summary>
    /// Contract: ContentRouterResult.TotalDuration must be >= TimeSpan.Zero.
    /// </summary>
    [Fact]
    public async Task ContentRouterResult_TotalDuration_NonNegative()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.Match(Right: r => r, Left: _ => null!);
        routerResult.TotalDuration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    /// <summary>
    /// Contract: RouteExecutionResult must have valid data.
    /// </summary>
    [Fact]
    public async Task RouteExecutionResult_HasValidData()
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("TestRoute", o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.Match(Right: r => r, Left: _ => null!);
        var routeResult = routerResult.RouteResults[0];

        routeResult.RouteName.ShouldNotBeNullOrWhiteSpace();
        routeResult.Result.ShouldNotBeNull();
        routeResult.Duration.ShouldBeGreaterThanOrEqualTo(TimeSpan.Zero);
        routeResult.ExecutedAtUtc.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    #endregion

    #region BuiltContentRouterDefinition Contract

    /// <summary>
    /// Contract: BuiltContentRouterDefinition.Routes must be non-null.
    /// </summary>
    [Fact]
    public void BuiltContentRouterDefinition_Routes_NeverNull()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        // Assert
        definition.Routes.ShouldNotBeNull();
    }

    /// <summary>
    /// Contract: BuiltContentRouterDefinition.RouteCount must match Routes.Count.
    /// </summary>
    [Fact]
    public void BuiltContentRouterDefinition_RouteCount_MatchesRoutes()
    {
        // Arrange & Act
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .When(o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("medium"))
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("low"))
            .Build();

        // Assert
        definition.RouteCount.ShouldBe(definition.Routes.Count);
        definition.RouteCount.ShouldBe(3);
    }

    /// <summary>
    /// Contract: BuiltContentRouterDefinition.HasDefaultRoute must be consistent with DefaultRoute.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuiltContentRouterDefinition_HasDefaultRoute_ConsistentWithDefaultRoute(bool hasDefault)
    {
        // Arrange
        var builder = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"));

        if (hasDefault)
        {
            builder = builder.Default(o => Right<EncinaError, string>("default"));
        }

        // Act
        var definition = builder.Build();

        // Assert
        definition.HasDefaultRoute.ShouldBe(hasDefault);
        (definition.DefaultRoute is not null).ShouldBe(hasDefault);
    }

    #endregion

    #region RouteDefinition Contract

    /// <summary>
    /// Contract: RouteDefinition.Matches must return the same result for the same input.
    /// </summary>
    [Fact]
    public void RouteDefinition_Matches_Deterministic()
    {
        // Arrange
        var route = new RouteDefinition<TestOrder, string>(
            "Test",
            o => o.Total > 100,
            (o, ct) => ValueTask.FromResult(Right<EncinaError, string>("result")));

        var order = new TestOrder { Total = 150 };

        // Act
        var result1 = route.Matches(order);
        var result2 = route.Matches(order);
        var result3 = route.Matches(order);

        // Assert
        result1.ShouldBe(result2);
        result2.ShouldBe(result3);
    }

    /// <summary>
    /// Contract: RouteDefinition.IsDefault must be false for non-default routes.
    /// </summary>
    [Fact]
    public void RouteDefinition_IsDefault_FalseForNonDefaultRoutes()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("result"))
            .Build();

        // Assert
        foreach (var route in definition.Routes)
        {
            route.IsDefault.ShouldBeFalse();
        }
    }

    /// <summary>
    /// Contract: RouteDefinition.IsDefault must be true for default route.
    /// </summary>
    [Fact]
    public void RouteDefinition_IsDefault_TrueForDefaultRoute()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("never"))
            .Default(o => Right<EncinaError, string>("default"))
            .Build();

        // Assert
        definition.DefaultRoute!.IsDefault.ShouldBeTrue();
    }

    #endregion

    public class TestOrder
    {
        public decimal Total { get; set; }
    }
}
