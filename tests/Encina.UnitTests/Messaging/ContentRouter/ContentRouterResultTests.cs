using Encina.Messaging.ContentRouter;
using Shouldly;

namespace Encina.UnitTests.Messaging.ContentRouter;

/// <summary>
/// Unit tests for <see cref="ContentRouterResult"/> and related classes.
/// </summary>
public sealed class ContentRouterResultTests
{
    #region ContentRouterResult Static Factory

    [Fact]
    public void Empty_ReturnsEmptyResult()
    {
        // Act
        var result = ContentRouterResult.Empty<string>();

        // Assert
        result.RouteResults.ShouldBeEmpty();
        result.MatchedRouteCount.ShouldBe(0);
        result.TotalDuration.ShouldBe(TimeSpan.Zero);
        result.UsedDefaultRoute.ShouldBeFalse();
        result.HasMatches.ShouldBeFalse();
    }

    #endregion

    #region ContentRouterResult<TResult> Tests

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var routeResults = new List<RouteExecutionResult<int>>
        {
            new("Route1", 10, TimeSpan.FromMilliseconds(100), DateTime.UtcNow)
        };
        var duration = TimeSpan.FromMilliseconds(150);

        // Act
        var result = new ContentRouterResult<int>(routeResults, 1, duration, usedDefaultRoute: true);

        // Assert
        result.RouteResults.ShouldBe(routeResults);
        result.MatchedRouteCount.ShouldBe(1);
        result.TotalDuration.ShouldBe(duration);
        result.UsedDefaultRoute.ShouldBeTrue();
        result.HasMatches.ShouldBeTrue();
    }

    [Fact]
    public void HasMatches_WhenNoMatches_ReturnsFalse()
    {
        // Arrange & Act
        var result = new ContentRouterResult<string>([], 0, TimeSpan.Zero, false);

        // Assert
        result.HasMatches.ShouldBeFalse();
    }

    [Fact]
    public void HasMatches_WhenHasMatches_ReturnsTrue()
    {
        // Arrange
        var routeResults = new List<RouteExecutionResult<string>>
        {
            new("Route1", "result", TimeSpan.FromMilliseconds(50), DateTime.UtcNow)
        };

        // Act
        var result = new ContentRouterResult<string>(routeResults, 1, TimeSpan.FromMilliseconds(50), false);

        // Assert
        result.HasMatches.ShouldBeTrue();
    }

    [Fact]
    public void MultipleRouteResults_StoresAll()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var routeResults = new List<RouteExecutionResult<int>>
        {
            new("Route1", 1, TimeSpan.FromMilliseconds(10), now),
            new("Route2", 2, TimeSpan.FromMilliseconds(20), now.AddMilliseconds(10)),
            new("Route3", 3, TimeSpan.FromMilliseconds(30), now.AddMilliseconds(30))
        };

        // Act
        var result = new ContentRouterResult<int>(routeResults, 3, TimeSpan.FromMilliseconds(60), false);

        // Assert
        result.RouteResults.Count.ShouldBe(3);
        result.MatchedRouteCount.ShouldBe(3);
    }

    #endregion

    #region RouteExecutionResult<TResult> Tests

    [Fact]
    public void RouteExecutionResult_Constructor_SetsAllProperties()
    {
        // Arrange
        var routeName = "TestRoute";
        var resultValue = "TestResult";
        var duration = TimeSpan.FromMilliseconds(100);
        var executedAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new RouteExecutionResult<string>(routeName, resultValue, duration, executedAt);

        // Assert
        result.RouteName.ShouldBe(routeName);
        result.Result.ShouldBe(resultValue);
        result.Duration.ShouldBe(duration);
        result.ExecutedAtUtc.ShouldBe(executedAt);
    }

    [Fact]
    public void RouteExecutionResult_WithNullResult_AllowsNull()
    {
        // Arrange & Act
        var result = new RouteExecutionResult<string?>("Route", null, TimeSpan.Zero, DateTime.UtcNow);

        // Assert
        result.Result.ShouldBeNull();
    }

    [Fact]
    public void RouteExecutionResult_WithValueType_Works()
    {
        // Arrange & Act
        var result = new RouteExecutionResult<int>("NumberRoute", 42, TimeSpan.FromMilliseconds(5), DateTime.UtcNow);

        // Assert
        result.Result.ShouldBe(42);
        result.RouteName.ShouldBe("NumberRoute");
    }

    [Fact]
    public void RouteExecutionResult_WithComplexType_Works()
    {
        // Arrange
        var complexResult = new TestComplexResult { Id = 1, Name = "Test" };

        // Act
        var result = new RouteExecutionResult<TestComplexResult>("ComplexRoute", complexResult, TimeSpan.FromMilliseconds(10), DateTime.UtcNow);

        // Assert
        result.Result.ShouldBe(complexResult);
        result.Result.Id.ShouldBe(1);
        result.Result.Name.ShouldBe("Test");
    }

    #endregion

    private sealed class TestComplexResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
