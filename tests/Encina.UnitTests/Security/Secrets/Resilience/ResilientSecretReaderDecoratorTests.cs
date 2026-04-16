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

public sealed class ResilientSecretReaderDecoratorTests
{
    private readonly ISecretReader _innerReader;
    private readonly SecretsResilienceOptions _options;
    private readonly ILogger<ResilientSecretReaderDecorator> _logger;

    public ResilientSecretReaderDecoratorTests()
    {
        _innerReader = Substitute.For<ISecretReader>();
        _options = new SecretsResilienceOptions();
        _logger = Substitute.For<ILogger<ResilientSecretReaderDecorator>>();
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_NullInner_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            null!, ResiliencePipeline.Empty, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("inner");
    }

    [Fact]
    public void Constructor_NullPipeline_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            _innerReader, null!, _options, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("pipeline");
    }

    [Fact]
    public void Constructor_NullOptions_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            _innerReader, ResiliencePipeline.Empty, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_Should_ThrowArgumentNullException()
    {
        var act = () => new ResilientSecretReaderDecorator(
            _innerReader, ResiliencePipeline.Empty, _options, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region GetSecretAsync (string) - Success Passthrough

    [Fact]
    public async Task GetSecretAsync_InnerReturnsRight_Should_ReturnRightUnchanged()
    {
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>("secret-value"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("secret-value"));
    }

    #endregion

    #region GetSecretAsync (string) - Non-Transient Error Passthrough

    [Fact]
    public async Task GetSecretAsync_InnerReturnsNonTransientError_Should_ReturnLeftWithoutRetry()
    {
        var notFoundError = SecretsErrors.NotFound("my-secret");
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(notFoundError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe(SecretsErrors.NotFoundCode)));
        await _innerReader.Received(1).GetSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSecretAsync_InnerReturnsAccessDenied_Should_ReturnLeftWithoutRetry()
    {
        var accessDeniedError = SecretsErrors.AccessDenied("my-secret");
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(accessDeniedError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe(SecretsErrors.AccessDeniedCode)));
        await _innerReader.Received(1).GetSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Transient Error Retry

    [Fact]
    public async Task GetSecretAsync_TransientErrorThenSuccess_Should_ReturnRightAfterRetry()
    {
        var callCount = 0;
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");

        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? ValueTask.FromResult<Either<EncinaError, string>>(transientError)
                    : ValueTask.FromResult<Either<EncinaError, string>>("secret-value");
            });

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.ShouldBe("secret-value"));
        callCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetSecretAsync_TransientErrorExhaustsRetries_Should_ReturnOriginalError()
    {
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, string>>(transientError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 2));

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe(SecretsErrors.ProviderUnavailableCode)));
        // 1 initial + 2 retries = 3 total calls
        await _innerReader.Received(3).GetSecretAsync("my-secret", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync (string) - Circuit Breaker

    [Fact]
    public async Task GetSecretAsync_CircuitBreakerOpen_Should_ReturnCircuitBreakerOpenError()
    {
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, string>>>(_ =>
                throw new BrokenCircuitException("Circuit is open"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region GetSecretAsync (string) - Timeout

    [Fact]
    public async Task GetSecretAsync_TimeoutExceeded_Should_ReturnResilienceTimeoutError()
    {
        _innerReader.GetSecretAsync("my-secret", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, string>>>(_ =>
                throw new TimeoutRejectedException("Operation timed out"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync("my-secret");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.ResilienceTimeoutCode)));
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Success Passthrough

    [Fact]
    public async Task GetSecretAsync_Typed_InnerReturnsRight_Should_ReturnRightUnchanged()
    {
        var expected = new TestConfig { Host = "localhost", Port = 5432 };
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v =>
        {
            v.Host.ShouldBe("localhost");
            v.Port.ShouldBe(5432);
        });
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Non-Transient Error Passthrough

    [Fact]
    public async Task GetSecretAsync_Typed_InnerReturnsNonTransientError_Should_ReturnLeftWithoutRetry()
    {
        var notFoundError = SecretsErrors.NotFound("config");
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, TestConfig>>(notFoundError));

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe(SecretsErrors.NotFoundCode)));
        await _innerReader.Received(1).GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Transient Error Retry

    [Fact]
    public async Task GetSecretAsync_Typed_TransientErrorThenSuccess_Should_ReturnRightAfterRetry()
    {
        var callCount = 0;
        var transientError = SecretsErrors.ProviderUnavailable("test-provider");
        var expected = new TestConfig { Host = "localhost" };

        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                callCount++;
                return callCount == 1
                    ? ValueTask.FromResult<Either<EncinaError, TestConfig>>(transientError)
                    : ValueTask.FromResult<Either<EncinaError, TestConfig>>(expected);
            });

        var decorator = CreateDecorator(BuildRetryPipeline(maxRetries: 3));

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsRight.ShouldBeTrue();
        result.IfRight(v => v.Host.ShouldBe("localhost"));
        callCount.ShouldBe(2);
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Circuit Breaker

    [Fact]
    public async Task GetSecretAsync_Typed_CircuitBreakerOpen_Should_ReturnCircuitBreakerOpenError()
    {
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, TestConfig>>>(_ =>
                throw new BrokenCircuitException("Circuit is open"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.CircuitBreakerOpenCode)));
    }

    #endregion

    #region GetSecretAsync<T> (typed) - Timeout

    [Fact]
    public async Task GetSecretAsync_Typed_TimeoutExceeded_Should_ReturnResilienceTimeoutError()
    {
        _innerReader.GetSecretAsync<TestConfig>("config", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, TestConfig>>>(_ =>
                throw new TimeoutRejectedException("Operation timed out"));

        var decorator = CreateDecorator(ResiliencePipeline.Empty);

        var result = await decorator.GetSecretAsync<TestConfig>("config");

        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c =>
            c.ShouldBe(SecretsErrors.ResilienceTimeoutCode)));
    }

    #endregion

    #region Helpers

    private ResilientSecretReaderDecorator CreateDecorator(ResiliencePipeline pipeline) =>
        new(_innerReader, pipeline, _options, _logger);

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

    private sealed class TestConfig
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
    }

    #endregion
}
