#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Resilience;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class ResilientSecretRotatorDecoratorTests
{
    private readonly ISecretRotator _innerRotator;
    private readonly SecretsResilienceOptions _options;
    private readonly ILogger<ResilientSecretRotatorDecorator> _logger;

    public ResilientSecretRotatorDecoratorTests()
    {
        _innerRotator = Substitute.For<ISecretRotator>();
        _options = new SecretsResilienceOptions();
        _logger = Substitute.For<ILogger<ResilientSecretRotatorDecorator>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretRotatorDecorator(
            null!, ResiliencePipeline.Empty, _options, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretRotatorDecorator(
            _innerRotator, null!, _options, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretRotatorDecorator(
            _innerRotator, ResiliencePipeline.Empty, null!, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretRotatorDecorator(
            _innerRotator, ResiliencePipeline.Empty, _options, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RotateSecretAsync - Success Passthrough

    [Fact]
    public async Task RotateSecretAsync_InnerReturnsRight_Should_ReturnRightUnchanged()
    {
        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region RotateSecretAsync - Non-Transient Error Passthrough

    [Fact]
    public async Task RotateSecretAsync_InnerReturnsNonTransientError_Should_ReturnLeftWithoutRetry()
    {
        var rotationError = SecretsErrors.RotationFailed("my-secret", "Permission denied");
        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(rotationError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsLeft.Should().BeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.Should().Be(SecretsErrors.RotationFailedCode)));
        await _innerRotator.Received(1).RotateSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    #endregion

    #region RotateSecretAsync - Transient Error Retry

    [Fact]
    public async Task RotateSecretAsync_TransientErrorThenSuccess_Should_ReturnRightAfterRetry()
    {
        var callCount = 0;
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");

        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? ValueTask.FromResult<Either<EncinaError, Unit>>(transientError)
                    : ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsRight.Should().BeTrue();
        callCount.Should().Be(2);
    }

    [Fact]
    public async Task RotateSecretAsync_TransientErrorExhaustsRetries_Should_ReturnOriginalError()
    {
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");
        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(transientError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 2));

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsLeft.Should().BeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.Should().Be(SecretsErrors.ProviderUnavailableCode)));
        // 1 initial + 2 retries = 3 total calls
        await _innerRotator.Received(3).RotateSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    #endregion

    #region RotateSecretAsync - Circuit Breaker

    [Fact]
    public async Task RotateSecretAsync_CircuitBreakerOpen_Should_ReturnCircuitBreakerOpenError()
    {
        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
                throw new BrokenCircuitException("Circuit is open"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsLeft.Should().BeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.Should().Be(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region RotateSecretAsync - Timeout

    [Fact]
    public async Task RotateSecretAsync_TimeoutExceeded_Should_ReturnResilienceTimeoutError()
    {
        _innerRotator.RotateSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
                throw new TimeoutRejectedException("Operation timed out"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.RotateSecretAsync("my-secret");

        result.IsLeft.Should().BeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.Should().Be(SecretsErrors.ResilienceTimeoutCode)));
    }

    #endregion

    #region Helpers

    private ResilientSecretRotatorDecorator CreateDecorator(ResiliencePipeline pipeline) =>
        new(_innerRotator, pipeline, _options, _logger);

    private static ResiliencePipeline BuildRetryPipeline(int maxRetries) =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = maxRetries,
                BackoffType = DelayBackoffType.Constant,
                Delay = TimeSpan.Zero,
                ShouldHandle = new PredicateBuilder().Handle<TransientSecretException>()
            })
            .Build();

    #endregion
}
