using System.Reflection;

using Encina.Database;
using Encina.Polly;
using Encina.Polly.Predicates;
using Encina.Testing;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Unit tests for <see cref="DatabaseCircuitBreakerPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public sealed class DatabaseCircuitBreakerPipelineBehaviorTests
{
    /// <summary>
    /// Clears the static circuit breaker cache to ensure test isolation.
    /// </summary>
    private static void ClearCircuitBreakerCache()
    {
        var cacheField = typeof(DatabaseCircuitBreakerPipelineBehavior<,>)
            .MakeGenericType(typeof(TestDbRequest), typeof(string))
            .GetField("_circuitBreakerCache", BindingFlags.Static | BindingFlags.NonPublic);

        if (cacheField?.GetValue(null) is System.Collections.IDictionary cache)
        {
            cache.Clear();
        }
    }

    private static ILogger<DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>> CreateLogger()
        => NullLoggerFactory.Instance.CreateLogger<DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>>();

    #region Constructor

    [Fact]
    public void Constructor_NullHealthMonitor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>(
                null!,
                new DatabaseCircuitBreakerOptions(),
                new DatabaseTransientErrorPredicate(new DatabaseCircuitBreakerOptions()),
                CreateLogger()));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>(
                Substitute.For<IDatabaseHealthMonitor>(),
                null!,
                new DatabaseTransientErrorPredicate(new DatabaseCircuitBreakerOptions()),
                CreateLogger()));
    }

    [Fact]
    public void Constructor_NullPredicate_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>(
                Substitute.For<IDatabaseHealthMonitor>(),
                new DatabaseCircuitBreakerOptions(),
                null!,
                CreateLogger()));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>(
                Substitute.For<IDatabaseHealthMonitor>(),
                new DatabaseCircuitBreakerOptions(),
                new DatabaseTransientErrorPredicate(new DatabaseCircuitBreakerOptions()),
                null!));
    }

    #endregion

    #region Handle — Fast Path (Circuit Open via Monitor)

    [Fact]
    public async Task Handle_CircuitOpenViaMonitor_ReturnsErrorImmediately()
    {
        // Arrange
        ClearCircuitBreakerCache();
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(true);
        monitor.ProviderName.Returns("test-provider");

        var behavior = CreateBehavior(monitor);
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult(Right<EncinaError, string>("success"));
        };

        // Act
        var result = await behavior.Handle(new TestDbRequest(), context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        nextCalled.ShouldBeFalse();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should be Left"),
            Left: error => error.Message.ShouldContain("circuit breaker is open"));
    }

    #endregion

    #region Handle — Successful Request

    [Fact]
    public async Task Handle_SuccessfulRequest_ReturnsResponse()
    {
        // Arrange
        ClearCircuitBreakerCache();
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(false);
        monitor.ProviderName.Returns("test-provider");

        var behavior = CreateBehavior(monitor);
        var context = Substitute.For<IRequestContext>();
        RequestHandlerCallback<string> next = () =>
            ValueTask.FromResult(Right<EncinaError, string>("success"));

        // Act
        var result = await behavior.Handle(new TestDbRequest(), context, next, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: value => value.ShouldBe("success"),
            Left: _ => throw new InvalidOperationException("Should be Right"));
    }

    #endregion

    #region Handle — Transient Error

    [Fact]
    public async Task Handle_TransientException_ReturnsError()
    {
        // Arrange
        ClearCircuitBreakerCache();
        var monitor = Substitute.For<IDatabaseHealthMonitor>();
        monitor.IsCircuitOpen.Returns(false);
        monitor.ProviderName.Returns("test-provider");

        var options = new DatabaseCircuitBreakerOptions
        {
            IncludeTimeouts = true
        };
        var predicate = new DatabaseTransientErrorPredicate(options);
        var behavior = CreateBehavior(monitor, options, predicate);
        var context = Substitute.For<IRequestContext>();

        RequestHandlerCallback<string> next = () =>
            throw new TimeoutException("db timeout");

        // Act
        var result = await behavior.Handle(new TestDbRequest(), context, next, CancellationToken.None);

        // Assert
        result.ShouldBeError();
    }

    #endregion

    #region Helpers

    private static DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string> CreateBehavior(
        IDatabaseHealthMonitor? monitor = null,
        DatabaseCircuitBreakerOptions? options = null,
        DatabaseTransientErrorPredicate? predicate = null)
    {
        monitor ??= Substitute.For<IDatabaseHealthMonitor>();
        options ??= new DatabaseCircuitBreakerOptions();
        predicate ??= new DatabaseTransientErrorPredicate(options);

        return new DatabaseCircuitBreakerPipelineBehavior<TestDbRequest, string>(
            monitor,
            options,
            predicate,
            CreateLogger());
    }

    private sealed record TestDbRequest : IRequest<string>;

    #endregion
}
