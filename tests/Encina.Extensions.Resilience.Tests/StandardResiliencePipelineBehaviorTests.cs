using Encina.Extensions.Resilience;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using Shouldly;
using Xunit;

namespace Encina.Extensions.Resilience.Tests;

/// <summary>
/// Unit tests for <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/>.
/// Tests the main behavior logic including success scenarios, error handling, and resilience strategy exceptions.
/// </summary>
public class StandardResiliencePipelineBehaviorTests
{
    [Fact]
    public async Task Handle_WithoutFailure_ShouldSucceed()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        var expectedResponse = new TestResponse { Value = "Success" };
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(expectedResponse);

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        _ = result.Match(
            Right: response => response.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_WithEncinaError_ShouldReturnError()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        var error = EncinaError.New("Test error");
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(error);

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e => e.Message.ShouldBe("Test error")
        );
    }

    [Fact]
    public async Task Handle_WithBrokenCircuitException_ShouldReturnCircuitBreakerError()
    {
        // Arrange
        var brokenCircuitEx = new BrokenCircuitException("Circuit is open");
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(brokenCircuitEx));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.ShouldContain("Circuit breaker is open");
                e.Message.ShouldContain("TestRequest");
                _ = e.Exception.Match(
                    Some: ex => ex.ShouldBeOfType<BrokenCircuitException>(),
                    None: () => throw new InvalidOperationException("Exception should be present")
                );
                return true;
            }
        );
    }

    [Fact]
    public async Task Handle_WithTimeoutRejectedException_ShouldReturnTimeoutError()
    {
        // Arrange
        var timeoutEx = new TimeoutRejectedException("Request timed out");
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(timeoutEx));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.ShouldContain("timed out");
                e.Message.ShouldContain("TestRequest");
                _ = e.Exception.Match(
                    Some: ex => ex.ShouldBeOfType<TimeoutRejectedException>(),
                    None: () => throw new InvalidOperationException("Exception should be present")
                );
                return true;
            }
        );
    }

    [Fact]
    public async Task Handle_WithUnexpectedException_ShouldReturnEncinaError()
    {
        // Arrange
        var unexpectedException = new InvalidOperationException("Unexpected error");
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(unexpectedException));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<EncinaError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.ShouldBe("Unexpected error");
                _ = e.Exception.Match(
                    Some: ex => ex.ShouldBeOfType<InvalidOperationException>(),
                    None: () => throw new InvalidOperationException("Exception should be present")
                );
                return true;
            }
        );
    }

    [Fact]
    public async Task Handle_WithCancellation_ShouldPropagateCancellation()
    {
        // Arrange
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });
        var logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);
        var context = Substitute.For<IRequestContext>();
        context.CorrelationId.Returns("test-correlation-id");

        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<TestResponse> nextStep = async () =>
        {
            await Task.Delay(1000, cts.Token);
            return new TestResponse();
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.ShouldBeError();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                _ = e.Exception.Match(
                    Some: ex => ex.ShouldBeOfType<TaskCanceledException>(),
                    None: () => throw new InvalidOperationException("Exception should be present")
                );
                return true;
            }
        );
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse
    {
        public string Value { get; init; } = string.Empty;
    }

    // Custom resilience strategy that throws a specific exception
    private sealed class ThrowExceptionStrategy : ResilienceStrategy
    {
        private readonly Exception _exception;

        public ThrowExceptionStrategy(Exception exception)
        {
            _exception = exception;
        }

        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            throw _exception;
        }
    }
}
