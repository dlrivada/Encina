using Encina.Messaging.ContentRouter;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Messaging.ContentRouter;

/// <summary>
/// Unit tests for <see cref="global::Encina.Messaging.ContentRouter.ContentRouter"/>.
/// </summary>
public sealed class ContentRouterTests
{
    private sealed record TestMessage(string Category, int Priority);

    #region Constructor

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var logger = NullLogger<global::Encina.Messaging.ContentRouter.ContentRouter>.Instance;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new global::Encina.Messaging.ContentRouter.ContentRouter(null!, logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new ContentRouterOptions();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new global::Encina.Messaging.ContentRouter.ContentRouter(options, null!));
    }

    #endregion

    #region RouteAsync - Basic Routing

    [Fact]
    public async Task RouteAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Arrange
        var router = CreateRouter();
        var message = new TestMessage("A", 1);

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => router.RouteAsync<TestMessage, string>(null!, message).AsTask());
    }

    [Fact]
    public async Task RouteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var router = CreateRouter();
        var definition = CreateDefinition<TestMessage, string>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(
            () => router.RouteAsync(definition, (TestMessage)null!).AsTask());
    }

    [Fact]
    public async Task RouteAsync_WithMatchingRoute_ExecutesHandler()
    {
        // Arrange
        var router = CreateRouter();
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("CategoryA", m => m.Category == "A", _ => "Result A")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].Result.ShouldBe("Result A");
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchingRouteAndDefaultRoute_UsesDefaultRoute()
    {
        // Arrange
        var router = CreateRouter();
        var defaultRoute = CreateRoute<TestMessage, string>("Default", _ => true, _ => "Default Result");
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("CategoryA", m => m.Category == "A", _ => "Result A")
            ],
            defaultRoute: defaultRoute);

        var message = new TestMessage("B", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.UsedDefaultRoute.ShouldBeTrue();
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].Result.ShouldBe("Default Result");
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchAndNoDefaultAndThrowOnNoMatchFalse_ReturnsEmptyResult()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("CategoryA", m => m.Category == "A", _ => "Result A")
            ]);

        var message = new TestMessage("B", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(0);
        routerResult.HasMatches.ShouldBeFalse();
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchAndNoDefaultAndThrowOnNoMatchTrue_ReturnsError()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = true };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("CategoryA", m => m.Category == "A", _ => "Result A")
            ]);

        var message = new TestMessage("B", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            code => code.ShouldBe(ContentRouterErrorCodes.NoMatchingRoute),
            () => throw new InvalidOperationException("Expected error code"));
    }

    #endregion

    #region RouteAsync - Multiple Routes

    [Fact]
    public async Task RouteAsync_WithAllowMultipleMatchesFalse_ExecutesOnlyFirstMatch()
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = false };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", _ => true, _ => "Result 1"),
                CreateRoute<TestMessage, string>("Route2", _ => true, _ => "Result 2")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].RouteName.ShouldBe("Route1");
    }

    [Fact]
    public async Task RouteAsync_WithAllowMultipleMatchesTrue_ExecutesAllMatchingRoutes()
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true, EvaluateInParallel = false };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", _ => true, _ => "Result 1"),
                CreateRoute<TestMessage, string>("Route2", _ => true, _ => "Result 2"),
                CreateRoute<TestMessage, string>("Route3", m => m.Category == "X", _ => "Result 3")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(2);
        routerResult.RouteResults[0].RouteName.ShouldBe("Route1");
        routerResult.RouteResults[1].RouteName.ShouldBe("Route2");
    }

    [Fact]
    public async Task RouteAsync_WithParallelExecution_ExecutesRoutesInParallel()
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true, EvaluateInParallel = true, MaxDegreeOfParallelism = 2 };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", _ => true, _ => "Result 1"),
                CreateRoute<TestMessage, string>("Route2", _ => true, _ => "Result 2")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(2);
    }

    #endregion

    #region RouteAsync - Error Handling

    [Fact]
    public async Task RouteAsync_WhenRouteHandlerReturnsError_ReturnsError()
    {
        // Arrange
        var router = CreateRouter();
        var error = EncinaErrors.Create("TEST_ERROR", "Test error");
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRouteWithError<TestMessage, string>("Route1", _ => true, error)
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.Message.ShouldContain("Test error");
    }

    [Fact]
    public async Task RouteAsync_WhenRouteHandlerThrowsException_ReturnsError()
    {
        // Arrange
        var router = CreateRouter();
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRouteWithException<TestMessage, string>("Route1", _ => true, new InvalidOperationException("Handler exception"))
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            code => code.ShouldBe(ContentRouterErrorCodes.RouteExecutionFailed),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task RouteAsync_WhenConditionThrowsException_SkipsRoute()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };
        var router = CreateRouter(options);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRouteWithConditionException<TestMessage, string>("Route1", new InvalidOperationException("Condition failed")),
                CreateRoute<TestMessage, string>("Route2", _ => true, _ => "Result 2")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].RouteName.ShouldBe("Route2");
    }

    [Fact]
    public async Task RouteAsync_WhenDefaultRouteReturnsError_ReturnsError()
    {
        // Arrange
        var router = CreateRouter();
        var error = EncinaErrors.Create("DEFAULT_ERROR", "Default route error");
        var defaultRoute = CreateRouteWithError<TestMessage, string>("Default", _ => true, error);
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", m => m.Category == "X", _ => "Result")
            ],
            defaultRoute: defaultRoute);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.Message.ShouldContain("Default route error");
    }

    [Fact]
    public async Task RouteAsync_WhenDefaultRouteThrowsException_ReturnsError()
    {
        // Arrange
        var router = CreateRouter();
        var defaultRoute = CreateRouteWithException<TestMessage, string>("Default", _ => true, new InvalidOperationException("Default failed"));
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", m => m.Category == "X", _ => "Result")
            ],
            defaultRoute: defaultRoute);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            code => code.ShouldBe(ContentRouterErrorCodes.RouteExecutionFailed),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task RouteAsync_WhenCancelled_ReturnsError()
    {
        // Arrange
        var router = CreateRouter();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRoute<TestMessage, string>("Route1", _ => true, _ =>
                {
                    cts.Token.ThrowIfCancellationRequested();
                    return "Result";
                })
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            code => code.ShouldBe(ContentRouterErrorCodes.RouterCancelled),
            () => throw new InvalidOperationException("Expected error code"));
    }

    [Fact]
    public async Task RouteAsync_WhenMultipleRoutesAndFirstFails_StopsOnError()
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true, EvaluateInParallel = false };
        var router = CreateRouter(options);
        var error = EncinaErrors.Create("FIRST_ERROR", "First route error");
        var definition = CreateDefinition<TestMessage, string>(
            routes:
            [
                CreateRouteWithError<TestMessage, string>("Route1", _ => true, error),
                CreateRoute<TestMessage, string>("Route2", _ => true, _ => "Result 2")
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsLeft.ShouldBeTrue();
        var resultError = result.LeftAsEnumerable().First();
        resultError.Message.ShouldContain("First route error");
    }

    #endregion

    #region RouteAsync - Unit Overload

    [Fact]
    public async Task RouteAsync_UnitOverload_DelegatesToGenericOverload()
    {
        // Arrange
        var router = CreateRouter();
        var definition = CreateDefinition<TestMessage, Unit>(
            routes:
            [
                CreateRoute<TestMessage, Unit>("Route1", _ => true, _ => unit)
            ]);

        var message = new TestMessage("A", 1);

        // Act
        var result = await router.RouteAsync(definition, message);

        // Assert
        result.IsRight.ShouldBeTrue();
        var routerResult = result.RightAsEnumerable().First();
        routerResult.RouteResults.Count.ShouldBe(1);
    }

    #endregion

    #region Helper Methods

    private static global::Encina.Messaging.ContentRouter.ContentRouter CreateRouter(ContentRouterOptions? options = null)
    {
        return new global::Encina.Messaging.ContentRouter.ContentRouter(
            options ?? new ContentRouterOptions(),
            NullLogger<global::Encina.Messaging.ContentRouter.ContentRouter>.Instance);
    }

    private static BuiltContentRouterDefinition<TMessage, TResult> CreateDefinition<TMessage, TResult>(
        IReadOnlyList<RouteDefinition<TMessage, TResult>>? routes = null,
        RouteDefinition<TMessage, TResult>? defaultRoute = null)
        where TMessage : class
    {
        // Use reflection to access internal constructor
        var type = typeof(BuiltContentRouterDefinition<TMessage, TResult>);
        var constructor = type.GetConstructor(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(IReadOnlyList<RouteDefinition<TMessage, TResult>>), typeof(RouteDefinition<TMessage, TResult>)],
            null);

        return (BuiltContentRouterDefinition<TMessage, TResult>)constructor!.Invoke([routes ?? [], defaultRoute]);
    }

    private static RouteDefinition<TMessage, TResult> CreateRoute<TMessage, TResult>(
        string name,
        Func<TMessage, bool> condition,
        Func<TMessage, TResult> handler)
        where TMessage : class
    {
        return new RouteDefinition<TMessage, TResult>(
            name,
            condition,
            (msg, _) => ValueTask.FromResult(Right<EncinaError, TResult>(handler(msg))),
            priority: 0);
    }

    private static RouteDefinition<TMessage, TResult> CreateRouteWithError<TMessage, TResult>(
        string name,
        Func<TMessage, bool> condition,
        EncinaError error)
        where TMessage : class
    {
        return new RouteDefinition<TMessage, TResult>(
            name,
            condition,
            (_, _) => ValueTask.FromResult(Left<EncinaError, TResult>(error)),
            priority: 0);
    }

    private static RouteDefinition<TMessage, TResult> CreateRouteWithException<TMessage, TResult>(
        string name,
        Func<TMessage, bool> condition,
        Exception exception)
        where TMessage : class
    {
        return new RouteDefinition<TMessage, TResult>(
            name,
            condition,
            (_, _) => throw exception,
            priority: 0);
    }

    private static RouteDefinition<TMessage, TResult> CreateRouteWithConditionException<TMessage, TResult>(
        string name,
        Exception exception)
        where TMessage : class
    {
        return new RouteDefinition<TMessage, TResult>(
            name,
            _ => throw exception,
            (_, _) => ValueTask.FromResult(Right<EncinaError, TResult>(default!)),
            priority: 0);
    }

    #endregion
}
