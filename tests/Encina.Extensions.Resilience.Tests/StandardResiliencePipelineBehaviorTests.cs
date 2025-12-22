using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;
using Polly.Registry;
using Polly.Timeout;
using Encina.Extensions.Resilience;
using Xunit;

namespace Encina.Extensions.Resilience.Tests;

/// <summary>
/// Unit tests for <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/>.
/// Tests the main behavior logic including success scenarios, error handling, and resilience strategy exceptions.
/// </summary>
public class StandardResiliencePipelineBehaviorTests
{
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>> _logger;
    private readonly StandardResiliencePipelineBehavior<TestRequest, TestResponse> _behavior;
    private readonly IRequestContext _context;

    public StandardResiliencePipelineBehaviorTests()
    {
        // Create a real resilience pipeline with minimal configuration
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddTimeout(TimeSpan.FromSeconds(10));
        });

        _pipelineProvider = registry;
        _logger = Substitute.For<ILogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>>();
        _behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(_pipelineProvider, _logger);
        _context = Substitute.For<IRequestContext>();
        _context.CorrelationId.Returns("test-correlation-id");
    }

    [Fact]
    public async Task Handle_WithoutFailure_ShouldSucceed()
    {
        // Arrange
        var request = new TestRequest();
        var expectedResponse = new TestResponse { Value = "Success" };
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<MediatorError, TestResponse>>(expectedResponse);

        // Act
        var result = await _behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsRight.Should().BeTrue();
        _ = result.Match(
            Right: response => response.Should().Be(expectedResponse),
            Left: _ => throw new InvalidOperationException("Should not be Left")
        );
    }

    [Fact]
    public async Task Handle_WithMediatorError_ShouldReturnError()
    {
        // Arrange
        var request = new TestRequest();
        var error = MediatorError.New("Test error");
        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<MediatorError, TestResponse>>(error);

        // Act
        var result = await _behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e => e.Message.Should().Be("Test error")
        );
    }

    [Fact]
    public async Task Handle_WithBrokenCircuitException_ShouldReturnCircuitBreakerError()
    {
        // Arrange
        var request = new TestRequest();
        var brokenCircuitEx = new BrokenCircuitException("Circuit is open");

        // Create a pipeline that always throws BrokenCircuitException
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(brokenCircuitEx));
        });

        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            registry,
            _logger);

        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<MediatorError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.Should().Contain("Circuit breaker is open");
                e.Message.Should().Contain("TestRequest");
                _ = e.Exception.Match(
                    Some: ex => ex.Should().BeOfType<BrokenCircuitException>(),
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
        var request = new TestRequest();
        var timeoutEx = new TimeoutRejectedException("Request timed out");

        // Create a pipeline that always throws TimeoutRejectedException
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(timeoutEx));
        });

        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            registry,
            _logger);

        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<MediatorError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.Should().Contain("timed out");
                e.Message.Should().Contain("TestRequest");
                _ = e.Exception.Match(
                    Some: ex => ex.Should().BeOfType<TimeoutRejectedException>(),
                    None: () => throw new InvalidOperationException("Exception should be present")
                );
                return true;
            }
        );
    }

    [Fact]
    public async Task Handle_WithUnexpectedException_ShouldReturnMediatorError()
    {
        // Arrange
        var request = new TestRequest();
        var unexpectedException = new InvalidOperationException("Unexpected error");

        // Create a pipeline that always throws an unexpected exception
        var registry = new ResiliencePipelineRegistry<string>();
        registry.TryAddBuilder("TestRequest", (builder, _) =>
        {
            builder.AddStrategy(_ => new ThrowExceptionStrategy(unexpectedException));
        });

        var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(
            registry,
            _logger);

        RequestHandlerCallback<TestResponse> nextStep = () => ValueTask.FromResult<Either<MediatorError, TestResponse>>(new TestResponse());

        // Act
        var result = await behavior.Handle(request, _context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                e.Message.Should().Be("Unexpected error");
                _ = e.Exception.Match(
                    Some: ex => ex.Should().BeOfType<InvalidOperationException>(),
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
        var request = new TestRequest();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerCallback<TestResponse> nextStep = async () =>
        {
            await Task.Delay(1000, cts.Token);
            return new TestResponse();
        };

        // Act
        var result = await _behavior.Handle(request, _context, nextStep, cts.Token);

        // Assert
        result.IsLeft.Should().BeTrue();
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Should not be Right"),
            Left: e =>
            {
                _ = e.Exception.Match(
                    Some: ex => ex.Should().BeOfType<TaskCanceledException>(),
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
