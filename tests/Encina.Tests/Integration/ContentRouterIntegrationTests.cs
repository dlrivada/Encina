using Encina.Messaging;
using Encina.Messaging.ContentRouter;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests.Integration;

/// <summary>
/// Integration tests for Content-Based Router with DI container.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ContentRouterIntegrationTests
{
    [Fact]
    public async Task ContentRouter_WithDependencyInjection_ResolvesCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();

        await using var provider = services.BuildServiceProvider();
        var router = provider.GetRequiredService<IContentRouter>();

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .Build();

        var order = new TestOrder { Total = 150 };

        // Act
        var result = await router.RouteAsync(definition, order);

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task ContentRouter_WithScopedServices_WorksAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();

        await using var provider = services.BuildServiceProvider();

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(o => Right<EncinaError, string>("high"))
            .Build();

        // Act - use multiple scopes
        var results = new List<Either<EncinaError, ContentRouterResult<string>>>();
        for (var i = 0; i < 3; i++)
        {
            await using var scope = provider.CreateAsyncScope();
            var router = scope.ServiceProvider.GetRequiredService<IContentRouter>();
            var order = new TestOrder { Total = 150 + i };
            var result = await router.RouteAsync(definition, order);
            results.Add(result);
        }

        // Assert
        results.ShouldAllBeSuccess();
    }

    [Fact]
    public async Task ContentRouter_WithHandlerAccessingServices_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();
        services.AddScoped<IOrderValidator, FakeOrderValidator>();

        await using var provider = services.BuildServiceProvider();

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 100)
            .RouteTo(async (o, ct) =>
            {
                // Simulate handler that would access services
                await Task.Delay(10, ct);
                return Right<EncinaError, string>($"Processed: {o.Total}");
            })
            .Build();

        // Act
        await using var scope = provider.CreateAsyncScope();
        var router = scope.ServiceProvider.GetRequiredService<IContentRouter>();
        var order = new TestOrder { Total = 150 };
        var result = await router.RouteAsync(definition, order);

        // Assert
        var routerResult = result.ShouldBeSuccess();
        routerResult.RouteResults[0].Result.ShouldBe("Processed: 150");
    }

    [Fact]
    public async Task ContentRouter_ConcurrentRouting_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();

        await using var provider = services.BuildServiceProvider();

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(10, ct);
                return Right<EncinaError, string>($"Order: {o.Id}");
            })
            .Build();

        // Act - concurrent requests
        var tasks = Enumerable.Range(0, 10)
            .Select(async i =>
            {
                await using var scope = provider.CreateAsyncScope();
                var router = scope.ServiceProvider.GetRequiredService<IContentRouter>();
                var order = new TestOrder { Id = Guid.NewGuid(), Total = 100 + i };
                return await router.RouteAsync(definition, order);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.ShouldAllBeSuccess();
        results.Length.ShouldBe(10);
    }

    [Fact]
    public async Task ContentRouter_WithComplexRoutingLogic_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();

        await using var provider = services.BuildServiceProvider();

        // Complex routing with multiple conditions
        var definition = ContentRouterBuilder.Create<TestOrder, OrderRouteResult>()
            .When("Premium", o => o.CustomerType == "Premium" && o.Total > 500)
            .WithPriority(1)
            .RouteTo(o => Right<EncinaError, OrderRouteResult>(
                new OrderRouteResult("PremiumHandler", o.Total * 0.9m)))

            .When("HighValue", o => o.Total > 1000)
            .WithPriority(2)
            .RouteTo(o => Right<EncinaError, OrderRouteResult>(
                new OrderRouteResult("HighValueHandler", o.Total * 0.95m)))

            .When("International", o => o.IsInternational)
            .WithPriority(3)
            .RouteTo(o => Right<EncinaError, OrderRouteResult>(
                new OrderRouteResult("InternationalHandler", o.Total + 50)))

            .Default(o => Right<EncinaError, OrderRouteResult>(
                new OrderRouteResult("StandardHandler", o.Total)))
            .Build();

        var testCases = new[]
        {
            (new TestOrder { CustomerType = "Premium", Total = 600 }, "PremiumHandler", 540m),
            (new TestOrder { Total = 1500 }, "HighValueHandler", 1425m),
            (new TestOrder { IsInternational = true, Total = 100 }, "InternationalHandler", 150m),
            (new TestOrder { Total = 50 }, "StandardHandler", 50m)
        };

        // Act & Assert
        await using var scope = provider.CreateAsyncScope();
        var router = scope.ServiceProvider.GetRequiredService<IContentRouter>();

        foreach (var (order, expectedHandler, expectedAmount) in testCases)
        {
            var result = await router.RouteAsync(definition, order);

            var routerResult = result.ShouldBeSuccess();
            var orderResult = routerResult.RouteResults[0].Result;

            orderResult.Handler.ShouldBe(expectedHandler);
            orderResult.ProcessedAmount.ShouldBe(expectedAmount);
        }
    }

    [Fact]
    public async Task ContentRouter_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(new ContentRouterOptions());
        services.AddScoped<IContentRouter, Messaging.ContentRouter.ContentRouter>();

        await using var provider = services.BuildServiceProvider();
        using var cts = new CancellationTokenSource();

        var definition = ContentRouterBuilder.Create<TestOrder, string>()
            .When(o => o.Total > 0)
            .RouteTo(async (o, ct) =>
            {
                await Task.Delay(5000, ct); // Long-running operation
                return Right<EncinaError, string>("completed");
            })
            .Build();

        // Act
        await using var scope = provider.CreateAsyncScope();
        var router = scope.ServiceProvider.GetRequiredService<IContentRouter>();
        var order = new TestOrder { Total = 100 };

        // Cancel after a short delay
        cts.CancelAfter(100);

        var result = await router.RouteAsync(definition, order, cts.Token);

        // Assert
        var error = result.ShouldBeError();
        error.Message.ShouldContain("cancelled");
    }

    public class TestOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public decimal Total { get; set; }
        public bool IsInternational { get; set; }
        public string? CustomerType { get; set; }
    }

    public record OrderRouteResult(string Handler, decimal ProcessedAmount);

    public interface IOrderValidator
    {
        bool IsValid(TestOrder order);
    }

    public class FakeOrderValidator : IOrderValidator
    {
        public bool IsValid(TestOrder order) => order.Total > 0;
    }
}
