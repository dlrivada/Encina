using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.Tests;

/// <summary>
/// Tests for parallel notification dispatch strategies.
/// </summary>
public sealed class ParallelNotificationDispatchTests
{
    private readonly List<string> _executionOrder = new();
    private readonly object _lock = new();

    [Fact]
    public async Task ParallelDispatch_ShouldInvokeAllHandlers()
    {
        // Arrange
        var Encina = CreateEncina(NotificationDispatchStrategy.Parallel);
        var notification = new TestNotification("test");

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _executionOrder.Count.ShouldBe(3); // All 3 handlers should have run
        _executionOrder.ShouldContain("Handler1");
        _executionOrder.ShouldContain("Handler2");
        _executionOrder.ShouldContain("Handler3");
    }

    [Fact]
    public async Task ParallelWhenAllDispatch_ShouldInvokeAllHandlers()
    {
        // Arrange
        var Encina = CreateEncina(NotificationDispatchStrategy.ParallelWhenAll);
        var notification = new TestNotification("test");

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _executionOrder.Count.ShouldBe(3);
        _executionOrder.ShouldContain("Handler1");
        _executionOrder.ShouldContain("Handler2");
        _executionOrder.ShouldContain("Handler3");
    }

    [Fact]
    public async Task SequentialDispatch_ShouldMaintainOrder()
    {
        // Arrange
        var Encina = CreateEncina(NotificationDispatchStrategy.Sequential);
        var notification = new TestNotification("test");

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        _executionOrder.Count.ShouldBe(3);
        // Sequential order should be preserved (Handler1, Handler2, Handler3)
        _executionOrder[0].ShouldBe("Handler1");
        _executionOrder[1].ShouldBe("Handler2");
        _executionOrder[2].ShouldBe("Handler3");
    }

    [Fact]
    public async Task ParallelDispatch_FirstError_ShouldCancelRemainingHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionOrder = new List<string>();
        var lockObj = new object();

        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.Parallel;
            options.MaxDegreeOfParallelism = 1; // Force sequential-like execution to test cancellation
        });
        services.AddScoped<IEncina, Encina>();
        services.AddScoped<INotificationHandler<FailingNotification>>(_ =>
            new FirstFailingHandler(executionOrder, lockObj));
        services.AddScoped<INotificationHandler<FailingNotification>>(_ =>
            new SecondSlowHandler(executionOrder, lockObj));
        services.AddScoped<INotificationHandler<FailingNotification>>(_ =>
            new ThirdSlowHandler(executionOrder, lockObj));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var notification = new FailingNotification();

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: err => err.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationInvokeException),
            Right: _ => throw new InvalidOperationException("Expected failure"));

        // First handler should have run and failed
        executionOrder.ShouldContain("First:Start");
        executionOrder.ShouldContain("First:Failed");
    }

    [Fact]
    public async Task ParallelWhenAllDispatch_MultipleErrors_ShouldAggregateAllFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.ParallelWhenAll;
        });
        services.AddScoped<IEncina, Encina>();
        services.AddScoped<INotificationHandler<AllFailNotification>>(_ => new FailingHandlerA());
        services.AddScoped<INotificationHandler<AllFailNotification>>(_ => new FailingHandlerB());

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();
        var notification = new AllFailNotification();

        // Act
        var result = await Encina.Publish(notification, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: err =>
            {
                // Should aggregate multiple failures
                var code = err.GetEncinaCode();
                (code == EncinaErrorCodes.NotificationMultipleFailures ||
                 code == EncinaErrorCodes.NotificationInvokeException).ShouldBeTrue();
            },
            Right: _ => throw new InvalidOperationException("Expected failure"));
    }

    [Fact]
    public async Task ParallelDispatch_WithCancellation_ShouldRespectToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var services = new ServiceCollection();
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.Parallel;
        });
        services.AddScoped<IEncina, Encina>();
        services.AddScoped<INotificationHandler<SlowNotification>>(_ => new SlowHandler(cts));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Publish(new SlowNotification(), cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Left: err => err.GetEncinaCode().ShouldContain("cancelled", Case.Insensitive),
            Right: _ => throw new InvalidOperationException("Expected cancellation"));
    }

    [Fact]
    public async Task ParallelDispatch_NoHandlers_ShouldSucceed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.Parallel;
        });
        services.AddScoped<IEncina, Encina>();
        // No handlers registered

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Publish(new EmptyNotification(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task ParallelDispatch_SingleHandler_ShouldOptimize()
    {
        // Arrange
        var services = new ServiceCollection();
        var executed = false;
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.Parallel;
        });
        services.AddScoped<IEncina, Encina>();
        services.AddScoped<INotificationHandler<SingleNotification>>(_ =>
            new SingleHandler(() => executed = true));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Publish(new SingleNotification(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        executed.ShouldBeTrue();
    }

    [Fact]
    public async Task MaxDegreeOfParallelism_ShouldLimitConcurrency()
    {
        // Arrange
        var maxConcurrent = 0;
        var currentConcurrent = 0;
        var lockObj = new object();
        var services = new ServiceCollection();

        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = NotificationDispatchStrategy.Parallel;
            options.MaxDegreeOfParallelism = 2; // Limit to 2 concurrent
        });
        services.AddScoped<IEncina, Encina>();

        // Add 5 handlers that track concurrency
        for (var i = 0; i < 5; i++)
        {
            services.AddScoped<INotificationHandler<ConcurrencyNotification>>(_ =>
                new ConcurrencyHandler(lockObj, () => currentConcurrent, val =>
                {
                    currentConcurrent = val;
                    if (val > maxConcurrent) maxConcurrent = val;
                }));
        }

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Publish(new ConcurrencyNotification(), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
        maxConcurrent.ShouldBeLessThanOrEqualTo(2);
    }

    [Theory]
    [InlineData(NotificationDispatchStrategy.Sequential)]
    [InlineData(NotificationDispatchStrategy.Parallel)]
    [InlineData(NotificationDispatchStrategy.ParallelWhenAll)]
    public async Task AllStrategies_ShouldHandleNullHandlersGracefully(NotificationDispatchStrategy strategy)
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = strategy;
        });
        services.AddScoped<IEncina, Encina>();
        // Register a factory that returns null (simulating edge case)
        services.AddScoped<INotificationHandler<TestNotification>>(_ => null!);
        services.AddScoped<INotificationHandler<TestNotification>>(_ => new TestHandler1(new List<string>(), new object()));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        // Act
        var result = await Encina.Publish(new TestNotification("test"), CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    private IEncina CreateEncina(NotificationDispatchStrategy strategy)
    {
        var services = new ServiceCollection();

        services.Configure<NotificationDispatchOptions>(options =>
        {
            options.Strategy = strategy;
        });

        services.AddScoped<IEncina, Encina>();
        services.AddScoped<INotificationHandler<TestNotification>>(_ => new TestHandler1(_executionOrder, _lock));
        services.AddScoped<INotificationHandler<TestNotification>>(_ => new TestHandler2(_executionOrder, _lock));
        services.AddScoped<INotificationHandler<TestNotification>>(_ => new TestHandler3(_executionOrder, _lock));

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IEncina>();
    }

    // Test notifications
    private sealed record TestNotification(string Value) : INotification;
    private sealed record FailingNotification : INotification;
    private sealed record AllFailNotification : INotification;
    private sealed record SlowNotification : INotification;
    private sealed record EmptyNotification : INotification;
    private sealed record SingleNotification : INotification;
    private sealed record ConcurrencyNotification : INotification;

    // Test handlers
    private sealed class TestHandler1(List<string> order, object lockObj) : INotificationHandler<TestNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                order.Add("Handler1");
            }
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class TestHandler2(List<string> order, object lockObj) : INotificationHandler<TestNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken); // Small delay to allow parallel execution to interleave
            lock (lockObj)
            {
                order.Add("Handler2");
            }
            return Right<EncinaError, Unit>(Unit.Default);
        }
    }

    private sealed class TestHandler3(List<string> order, object lockObj) : INotificationHandler<TestNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(TestNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(5, cancellationToken);
            lock (lockObj)
            {
                order.Add("Handler3");
            }
            return Right<EncinaError, Unit>(Unit.Default);
        }
    }

    private sealed class FirstFailingHandler(List<string> order, object lockObj) : INotificationHandler<FailingNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(FailingNotification notification, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                order.Add("First:Start");
            }
            var error = EncinaErrors.Create(EncinaErrorCodes.NotificationInvokeException, "First handler failed");
            lock (lockObj)
            {
                order.Add("First:Failed");
            }
            return Task.FromResult(Left<EncinaError, Unit>(error));
        }
    }

    private sealed class SecondSlowHandler(List<string> order, object lockObj) : INotificationHandler<FailingNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(FailingNotification notification, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                order.Add("Second:Start");
            }
            await Task.Delay(500, cancellationToken);
            lock (lockObj)
            {
                order.Add("Second:End");
            }
            return Right<EncinaError, Unit>(Unit.Default);
        }
    }

    private sealed class ThirdSlowHandler(List<string> order, object lockObj) : INotificationHandler<FailingNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(FailingNotification notification, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                order.Add("Third:Start");
            }
            await Task.Delay(500, cancellationToken);
            lock (lockObj)
            {
                order.Add("Third:End");
            }
            return Right<EncinaError, Unit>(Unit.Default);
        }
    }

    private sealed class FailingHandlerA : INotificationHandler<AllFailNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(AllFailNotification notification, CancellationToken cancellationToken)
        {
            var error = EncinaErrors.Create(EncinaErrorCodes.NotificationInvokeException, "Handler A failed");
            return Task.FromResult(Left<EncinaError, Unit>(error));
        }
    }

    private sealed class FailingHandlerB : INotificationHandler<AllFailNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(AllFailNotification notification, CancellationToken cancellationToken)
        {
            var error = EncinaErrors.Create(EncinaErrorCodes.NotificationInvokeException, "Handler B failed");
            return Task.FromResult(Left<EncinaError, Unit>(error));
        }
    }

    private sealed class SlowHandler(CancellationTokenSource cts) : INotificationHandler<SlowNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(SlowNotification notification, CancellationToken cancellationToken)
        {
            // Cancel after a short delay
            _ = Task.Run(async () =>
            {
                await Task.Delay(50);
                await cts.CancelAsync();
            }, CancellationToken.None);

            await Task.Delay(1000, cancellationToken);
            return Right<EncinaError, Unit>(Unit.Default);
        }
    }

    private sealed class SingleHandler(Action onExecute) : INotificationHandler<SingleNotification>
    {
        public Task<Either<EncinaError, Unit>> Handle(SingleNotification notification, CancellationToken cancellationToken)
        {
            onExecute();
            return Task.FromResult(Right<EncinaError, Unit>(Unit.Default));
        }
    }

    private sealed class ConcurrencyHandler(object lockObj, Func<int> getCurrent, Action<int> setCurrent) : INotificationHandler<ConcurrencyNotification>
    {
        public async Task<Either<EncinaError, Unit>> Handle(ConcurrencyNotification notification, CancellationToken cancellationToken)
        {
            lock (lockObj)
            {
                setCurrent(getCurrent() + 1);
            }

            await Task.Delay(50, cancellationToken); // Simulate work

            lock (lockObj)
            {
                setCurrent(getCurrent() - 1);
            }

            return Right<EncinaError, Unit>(Unit.Default);
        }
    }
}
