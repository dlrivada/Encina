using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA1861 // Prefer static readonly array for test data

namespace Encina.Tests.PropertyTests;

/// <summary>
/// Property-based tests for Content-Based Router pattern.
/// Verifies invariants and properties that should hold across various inputs.
/// </summary>
public sealed class ContentRouterPropertyTests
{
    private readonly ContentRouterOptions _options = new();
    private readonly ILogger<Messaging.ContentRouter.ContentRouter> _logger =
        Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();

    #region Route Selection Invariants

    /// <summary>
    /// Property: First matching route wins (when AllowMultipleMatches is false).
    /// Invariant: Only one route executes regardless of how many match.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task FirstMatchWins_OnlyOneRouteExecutes(int matchingRouteCount)
    {
        // Arrange
        var executedRoutes = new List<string>();
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < matchingRouteCount; i++)
        {
            var routeName = $"Route_{i}";
            builder = builder.When(routeName, o => o.Total > 0)
                .RouteTo(o =>
                {
                    executedRoutes.Add(routeName);
                    return Right<EncinaError, string>(routeName);
                });
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.ShouldBeSuccess();
        executedRoutes.Count.ShouldBe(1);
        executedRoutes[0].ShouldBe("Route_0");
    }

    /// <summary>
    /// Property: Priority ordering is respected.
    /// Invariant: Lower priority value routes execute before higher priority value routes.
    /// </summary>
    [Theory]
    [InlineData(new[] { 5, 3, 1, 4, 2 })]
    [InlineData(new[] { 100, 1, 50 })]
    [InlineData(new[] { 10, 20, 30 })]
    public async Task PriorityOrdering_LowestPriorityExecutesFirst(int[] priorities)
    {
        // Arrange
        var executedRoute = "";
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        foreach (var priority in priorities)
        {
            var p = priority;
            builder = builder.When($"Priority_{p}", o => o.Total > 0)
                .WithPriority(p)
                .RouteTo(o =>
                {
                    executedRoute = $"Priority_{p}";
                    return Right<EncinaError, string>(executedRoute);
                });
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.ShouldBeSuccess();
        var expectedPriority = priorities.Min();
        executedRoute.ShouldBe($"Priority_{expectedPriority}");
    }

    /// <summary>
    /// Property: All matching routes execute when AllowMultipleMatches is true.
    /// Invariant: Every route whose condition returns true is executed.
    /// </summary>
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 5)]
    [InlineData(10, 7)]
    public async Task MultipleMatches_AllMatchingRoutesExecute(int totalRoutes, int matchingRoutes)
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);
        var executedCount = 0;

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < totalRoutes; i++)
        {
            var shouldMatch = i < matchingRoutes;
            builder = builder.When($"Route_{i}", o => shouldMatch)
                .RouteTo(o =>
                {
                    Interlocked.Increment(ref executedCount);
                    return Right<EncinaError, string>($"Route_{i}");
                });
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        executedCount.ShouldBe(matchingRoutes);
        routerResult.MatchedRouteCount.ShouldBe(matchingRoutes);
    }

    #endregion

    #region Default Route Invariants

    /// <summary>
    /// Property: Default route only executes when no other routes match.
    /// Invariant: If any route matches, default is NOT used.
    /// </summary>
    [Theory]
    [InlineData(true, false)]  // Has match -> default not used
    [InlineData(false, true)]  // No match -> default used
    public async Task DefaultRoute_OnlyUsedWhenNoMatch(bool hasMatchingRoute, bool expectDefaultUsed)
    {
        // Arrange
        var defaultExecuted = false;
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();

        if (hasMatchingRoute)
        {
            builder = builder.When("MatchingRoute", o => o.Total > 0)
                .RouteTo(o => Right<EncinaError, string>("matched"));
        }
        else
        {
            builder = builder.When("NonMatchingRoute", o => o.Total > 1000)
                .RouteTo(o => Right<EncinaError, string>("never"));
        }

        builder = builder.Default(o =>
        {
            defaultExecuted = true;
            return Right<EncinaError, string>("default");
        });

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        defaultExecuted.ShouldBe(expectDefaultUsed);
        routerResult.UsedDefaultRoute.ShouldBe(expectDefaultUsed);
    }

    #endregion

    #region Duration Tracking Invariants

    /// <summary>
    /// Property: Total duration >= sum of individual route durations.
    /// Invariant: Total time accounts for all route executions.
    /// </summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task DurationTracking_TotalGreaterThanIndividual(int routeCount)
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < routeCount; i++)
        {
            builder = builder.When($"Route_{i}", o => true)
                .RouteTo(async (o, ct) =>
                {
                    await Task.Delay(10, ct);
                    return Right<EncinaError, string>($"Route_{i}");
                });
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        var sumOfDurations = routerResult.RouteResults.Sum(r => r.Duration.TotalMilliseconds);
        routerResult.TotalDuration.TotalMilliseconds.ShouldBeGreaterThanOrEqualTo(sumOfDurations * 0.9); // Allow some margin
    }

    #endregion

    #region Error Handling Invariants

    /// <summary>
    /// Property: Handler errors propagate correctly.
    /// Invariant: If any handler returns Left, the router returns Left.
    /// </summary>
    [Theory]
    [InlineData(0)]  // First route fails
    [InlineData(2)]  // Third route fails
    [InlineData(4)]  // Fifth route fails
    public async Task HandlerErrors_PropagateCorrectly(int failingRouteIndex)
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);
        const int totalRoutes = 5;

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < totalRoutes; i++)
        {
            var index = i;
            builder = builder.When($"Route_{i}", o => true)
                .RouteTo(o => index == failingRouteIndex
                    ? Left<EncinaError, string>(EncinaError.New($"Route {index} failed"))
                    : Right<EncinaError, string>($"Route_{index}"));
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain($"Route {failingRouteIndex} failed");
    }

    /// <summary>
    /// Property: Condition exceptions are handled gracefully.
    /// Invariant: A throwing condition skips that route but doesn't crash.
    /// </summary>
    [Theory]
    [InlineData(0)]  // First condition throws
    [InlineData(1)]  // Second condition throws
    public async Task ConditionExceptions_SkipRoute(int throwingRouteIndex)
    {
        // Arrange
        var router = new Messaging.ContentRouter.ContentRouter(_options, _logger);
        const int totalRoutes = 3;

        var builder = ContentRouterBuilder.Create<TestOrder, string>();
        for (var i = 0; i < totalRoutes; i++)
        {
            var index = i;
            builder = builder.When($"Route_{i}", o =>
            {
                if (index == throwingRouteIndex)
                    throw new InvalidOperationException("Condition error");
                return true;
            })
            .RouteTo(o => Right<EncinaError, string>($"Route_{index}"));
        }

        var definition = builder.Build();
        var order = new TestOrder { Total = 100 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        // The first non-throwing route should execute
        routerResult.RouteResults.Count.ShouldBe(1);
    }

    #endregion

    #region Route Count Invariants

    /// <summary>
    /// Property: MatchedRouteCount reflects actual matching routes.
    /// Invariant: MatchedRouteCount equals number of routes whose condition returned true.
    /// </summary>
    [Theory]
    [InlineData(100, 0)]
    [InlineData(100, 50)]
    [InlineData(100, 200)]
    [InlineData(100, 100)]
    public async Task MatchedRouteCount_ReflectsActualMatches(int threshold, decimal orderTotal)
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("AboveThreshold", o => o.Total > threshold)
            .RouteTo(o => Right<EncinaError, string>("above"))
            .When("BelowThreshold", o => o.Total <= threshold)
            .RouteTo(o => Right<EncinaError, string>("below"))
            .Build();

        var order = new TestOrder { Total = orderTotal };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        // Exactly one condition should match
        routerResult.MatchedRouteCount.ShouldBe(1);
    }

    #endregion

    public class TestOrder
    {
        public decimal Total { get; set; }
        public bool IsInternational { get; set; }
    }
}
