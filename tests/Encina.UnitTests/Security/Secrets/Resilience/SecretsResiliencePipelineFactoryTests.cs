using Encina.Security.Secrets.Resilience;
using Shouldly;
using Microsoft.Extensions.Logging;
using NSubstitute;

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
