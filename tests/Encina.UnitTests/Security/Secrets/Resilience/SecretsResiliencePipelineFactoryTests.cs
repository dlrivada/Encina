using Encina.Security.Secrets.Resilience;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsResiliencePipelineFactoryTests
{
    private readonly ILogger _logger;

    public SecretsResiliencePipelineFactoryTests()
    {
        _logger = Substitute.For<ILogger>();
    }

    #region Create - Valid Arguments

    [Fact]
    public void Create_WithDefaultOptions_Should_ReturnNonNullPipeline()
    {
        var options = new SecretsResilienceOptions();
        var state = new SecretsCircuitBreakerState();

        var pipeline = SecretsResiliencePipelineFactory.Create(options, state, _logger);

        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithZeroRetries_Should_ReturnNonNullPipeline()
    {
        var options = new SecretsResilienceOptions { MaxRetryAttempts = 0 };
        var state = new SecretsCircuitBreakerState();

        var pipeline = SecretsResiliencePipelineFactory.Create(options, state, _logger);

        pipeline.ShouldNotBeNull();
    }

    [Fact]
    public void Create_WithCustomOptions_Should_ReturnNonNullPipeline()
    {
        var options = new SecretsResilienceOptions
        {
            MaxRetryAttempts = 5,
            RetryBaseDelay = TimeSpan.FromSeconds(1),
            RetryMaxDelay = TimeSpan.FromMinutes(1),
            CircuitBreakerFailureRatio = 0.8,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(120),
            CircuitBreakerMinimumThroughput = 20,
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1),
            OperationTimeout = TimeSpan.FromSeconds(60)
        };
        var state = new SecretsCircuitBreakerState();

        var pipeline = SecretsResiliencePipelineFactory.Create(options, state, _logger);

        pipeline.ShouldNotBeNull();
    }

    #endregion

    #region Create - Pipeline behaviour

    [Fact]
    public async Task Execute_WhenATransientFailureIsRetried_LogsTheExceptionTypeNeverItsMessage()
    {
        const string Sentinel = "vault secret-for-patient-12345 unreachable";
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var options = new SecretsResilienceOptions
        {
            MaxRetryAttempts = 1,
            RetryBaseDelay = TimeSpan.Zero,
            RetryMaxDelay = TimeSpan.FromMilliseconds(10)
        };
        var pipeline = SecretsResiliencePipelineFactory.Create(options, new SecretsCircuitBreakerState(), _logger);
        var attempts = 0;

        var result = await pipeline.ExecuteAsync(async _ =>
        {
            await Task.Yield();
            if (Interlocked.Increment(ref attempts) == 1)
            {
                throw new TransientSecretException(EncinaErrors.Create("secrets.provider_unavailable", Sentinel));
            }

            return 42;
        });

        result.ShouldBe(42);
        attempts.ShouldBe(2);
        var logged = _logger.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(ILogger.Log))
            .Select(call => call.GetArguments()[2]?.ToString() ?? string.Empty)
            .ToList();
        logged.ShouldContain(text => text.Contains(nameof(TransientSecretException)));
        logged.ShouldNotContain(text => text.Contains(Sentinel));
    }

    [Fact]
    public async Task Execute_WhenRepeatedFailuresTripTheBreaker_StateBecomesOpened()
    {
        var options = new SecretsResilienceOptions
        {
            MaxRetryAttempts = 0,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30)
        };
        var state = new SecretsCircuitBreakerState();
        var pipeline = SecretsResiliencePipelineFactory.Create(options, state, _logger);

        for (var i = 0; i < 2; i++)
        {
            await Should.ThrowAsync<IOException>(async () =>
                await pipeline.ExecuteAsync<int>(_ => throw new IOException("disk")));
        }

        state.State.ShouldBe(CircuitBreakerStateValue.Opened);
    }

    [Fact]
    public async Task Execute_WhenTheOperationExceedsTheTimeout_ThrowsTimeoutRejected()
    {
        var options = new SecretsResilienceOptions
        {
            MaxRetryAttempts = 0,
            OperationTimeout = TimeSpan.FromMilliseconds(50)
        };
        var pipeline = SecretsResiliencePipelineFactory.Create(options, new SecretsCircuitBreakerState(), _logger);

        await Should.ThrowAsync<global::Polly.Timeout.TimeoutRejectedException>(async () =>
            await pipeline.ExecuteAsync(async token => await Task.Delay(Timeout.InfiniteTimeSpan, token)));
    }

    #endregion

    #region Create - Null Arguments

    [Fact]
    public void Create_NullOptions_Should_ThrowArgumentNullException()
    {
        var state = new SecretsCircuitBreakerState();

        var act = () => SecretsResiliencePipelineFactory.Create(null!, state, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Create_NullCircuitBreakerState_Should_ThrowArgumentNullException()
    {
        var options = new SecretsResilienceOptions();

        var act = () => SecretsResiliencePipelineFactory.Create(options, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("circuitBreakerState");
    }

    [Fact]
    public void Create_NullLogger_Should_ThrowArgumentNullException()
    {
        var options = new SecretsResilienceOptions();
        var state = new SecretsCircuitBreakerState();

        var act = () => SecretsResiliencePipelineFactory.Create(options, state, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
