using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.ContentRouter;

public sealed class ContentRouterTests
{
    private readonly ContentRouterOptions _options;
    private readonly ILogger<Messaging.ContentRouter.ContentRouter> _logger;
    private readonly Messaging.ContentRouter.ContentRouter _sut;

    public ContentRouterTests()
    {
        _options = new ContentRouterOptions();
        _logger = Substitute.For<ILogger<Messaging.ContentRouter.ContentRouter>>();
        _sut = new Messaging.ContentRouter.ContentRouter(_options, _logger);
    }

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new Messaging.ContentRouter.ContentRouter(null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new Messaging.ContentRouter.ContentRouter(_options, null!));
    }

    [Fact]
    public async Task RouteAsync_WithNullDefinition_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.RouteAsync<TestOrder, string>(null!, new TestOrder()));
    }

    [Fact]
    public async Task RouteAsync_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .Build();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await _sut.RouteAsync(definition, null!));
    }

    [Fact]
    public async Task RouteAsync_WithMatchingRoute_ExecutesHandler()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high-value"))
            .Build();

        var order = new TestOrder { Total = 150 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.MatchedRouteCount.ShouldBe(1);
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].Result.ShouldBe("high-value");
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchingRoute_ReturnsError()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("very-high"))
            .Build();

        var order = new TestOrder { Total = 50 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("No matching route");
    }

    [Fact]
    public async Task RouteAsync_WithNoMatchAndThrowOnNoMatchFalse_ReturnsEmptyResult()
    {
        // Arrange
        var options = new ContentRouterOptions { ThrowOnNoMatch = false };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("very-high"))
            .Build();

        var order = new TestOrder { Total = 50 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.HasMatches.ShouldBeFalse();
        routerResult.MatchedRouteCount.ShouldBe(0);
    }

    [Fact]
    public async Task RouteAsync_WithDefaultRoute_UsesDefaultWhenNoMatch()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 1000)
            .RouteTo(o => Right<EncinaError, string>("very-high"))
            .Default(o => Right<EncinaError, string>("standard"))
            .Build();

        var order = new TestOrder { Total = 50 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.UsedDefaultRoute.ShouldBeTrue();
        routerResult.RouteResults.Count.ShouldBe(1);
        routerResult.RouteResults[0].Result.ShouldBe("standard");
    }

    [Fact]
    public async Task RouteAsync_WithMultipleRoutes_UsesFirstMatchByDefault()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("over-100"))
            .When(o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("over-50"))
            .Build();

        var order = new TestOrder { Total = 150 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.MatchedRouteCount.ShouldBe(1);
        routerResult.RouteResults[0].Result.ShouldBe("over-100");
    }

    [Fact]
    public async Task RouteAsync_WithMultipleMatchesEnabled_ExecutesAllMatches()
    {
        // Arrange
        var options = new ContentRouterOptions { AllowMultipleMatches = true };
        var router = new Messaging.ContentRouter.ContentRouter(options, _logger);

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("High Value", o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("over-100"))
            .When("Medium Value", o => o.Total > 50)
            .RouteTo(o => Right<EncinaError, string>("over-50"))
            .Build();

        var order = new TestOrder { Total = 150 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.MatchedRouteCount.ShouldBe(2);
        routerResult.RouteResults.Count.ShouldBe(2);
    }

    [Fact]
    public async Task RouteAsync_WithHandlerError_ReturnsError()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo((o, ct) => ValueTask.FromResult(Left<EncinaError, string>(EncinaError.New("Handler failed"))))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldBe("Handler failed");
    }

    [Fact]
    public async Task RouteAsync_WithHandlerException_ReturnsError()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo((o, ct) => throw new InvalidOperationException("Unexpected error"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("Unexpected error");
    }

    [Fact]
    public async Task RouteAsync_WithCancellation_ReturnsCancelledError()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(1000, ct);
                return Right<EncinaError, string>("result");
            })
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        cts.Cancel();
        var result = await _sut.RouteAsync(definition, order, cts.Token);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("cancelled");
    }

    [Fact]
    public async Task RouteAsync_WithRoutePriority_ExecutesHighestPriorityFirst()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .WithPriority(10)
            .RouteTo(o => Right<EncinaError, string>("low-priority"))
            .When(o => o.Total > 0)
            .WithPriority(1)
            .RouteTo(o => Right<EncinaError, string>("high-priority"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.RouteResults[0].Result.ShouldBe("high-priority");
    }

    [Fact]
    public async Task RouteAsync_WithNamedRoute_IncludesRouteNameInResult()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When("HighValueOrders", o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high-value"))
            .Build();

        var order = new TestOrder { Total = 150 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.RouteResults[0].RouteName.ShouldBe("HighValueOrders");
    }

    [Fact]
    public async Task RouteAsync_WithAsyncHandler_ExecutesCorrectly()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, string>($"Total: {o.Total}");
            })
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.RouteResults[0].Result.ShouldBe("Total: 100");
    }

    [Fact]
    public async Task RouteAsync_TracksExecutionDuration()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(50, ct);
                return Right<EncinaError, string>("done");
            })
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.TotalDuration.TotalMilliseconds.ShouldBeGreaterThan(40);
        routerResult.RouteResults[0].Duration.TotalMilliseconds.ShouldBeGreaterThan(40);
    }

    [Fact]
    public async Task RouteAsync_WithConditionException_SkipsRoute()
    {
        // Arrange
        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => throw new InvalidOperationException("Condition failed"))
            .RouteTo(o => Right<EncinaError, string>("never-reached"))
            .When(o => o.Total > 0)
            .RouteTo(o => Right<EncinaError, string>("fallback"))
            .Build();

        var order = new TestOrder { Total = 100 };

        // Act
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.RouteResults[0].Result.ShouldBe("fallback");
    }

    [Fact]
    public async Task RouteAsync_WithUnitResult_WorksCorrectly()
    {
        // Arrange
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
        var result = await _sut.RouteAsync(definition, order);

        // Assert
        result.ShouldBeSuccess();
        executed.ShouldBeTrue();
    }

    public class TestOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal Total { get; set; }
        public bool IsInternational { get; set; }
        public string? CustomerType { get; set; }
    }
}
