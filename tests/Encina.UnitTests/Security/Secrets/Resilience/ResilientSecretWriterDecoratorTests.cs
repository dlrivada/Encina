#pragma warning disable CA2012 // ValueTask instances used in NSubstitute mock setup

using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Resilience;
using Shouldly;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class ResilientSecretWriterDecoratorTests
{
    private readonly ISecretWriter _innerWriter;
    private readonly SecretsResilienceOptions _options;
    private readonly ILogger<ResilientSecretWriterDecorator> _logger;

    public ResilientSecretWriterDecoratorTests()
    {
        _innerWriter = Substitute.For<ISecretWriter>();
        _options = new SecretsResilienceOptions();
        _logger = Substitute.For<ILogger<ResilientSecretWriterDecorator>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            null!, ResiliencePipeline.Empty, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            _innerWriter, null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            _innerWriter, ResiliencePipeline.Empty, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretWriterDecorator(
            _innerWriter, ResiliencePipeline.Empty, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region SetSecretAsync - Success Passthrough

    [Fact]
    public async Task SetSecretAsync_InnerReturnsRight_Should_ReturnRightUnchanged()
    {
        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region SetSecretAsync - Non-Transient Error Passthrough

    [Fact]
    public async Task SetSecretAsync_InnerReturnsNonTransientError_Should_ReturnLeftWithoutRetry()
    {
        var accessDeniedError = SecretsErrors.AccessDenied("my-secret");
        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(accessDeniedError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsLeft.ShouldBeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.AccessDeniedCode)));
        await _innerWriter.Received(1).SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - Transient Error Retry

    [Fact]
    public async Task SetSecretAsync_TransientErrorThenSuccess_Should_ReturnRightAfterRetry()
    {
        var callCount = 0;
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");

        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? ValueTask.FromResult<Either<EncinaError, Unit>>(transientError)
                    : ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default);
            });

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsRight.ShouldBeTrue();
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task SetSecretAsync_TransientErrorExhaustsRetries_Should_ReturnOriginalError()
    {
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");
        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(transientError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 2));

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsLeft.ShouldBeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.ProviderUnavailableCode)));
        // 1 initial + 2 retries = 3 total calls
        await _innerWriter.Received(3).SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>());
    }

    #endregion

    #region SetSecretAsync - Circuit Breaker

    [Fact]
    public async Task SetSecretAsync_CircuitBreakerOpen_Should_ReturnCircuitBreakerOpenError()
    {
        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
                throw new BrokenCircuitException("Circuit is open"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsLeft.ShouldBeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region SetSecretAsync - Timeout

    [Fact]
    public async Task SetSecretAsync_TimeoutExceeded_Should_ReturnResilienceTimeoutError()
    {
        _innerWriter.SetSecretAsync("my-secret", "my-value", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
                throw new TimeoutRejectedException("Operation timed out"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.SetSecretAsync("my-secret", "my-value");

        result.IsLeft.ShouldBeTrue();
        _ = result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.ResilienceTimeoutCode)));
    }

    #endregion

    #region Helpers

    private ResilientSecretWriterDecorator CreateDecorator(ResiliencePipeline pipeline) =>
        new(_innerWriter, pipeline, _options, _logger);

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
