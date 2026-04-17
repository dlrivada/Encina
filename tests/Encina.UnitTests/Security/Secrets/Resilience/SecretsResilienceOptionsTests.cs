using Encina.Security.Secrets.Resilience;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsResilienceOptionsTests
{
    #region Default Values

    [Fact]
    public void MaxRetryAttempts_Should_DefaultToThree()
    {
        var options = new SecretsResilienceOptions();

        options.MaxRetryAttempts.ShouldBe(3);
    }

    [Fact]
    public void RetryBaseDelay_Should_DefaultToTwoSeconds()
    {
        var options = new SecretsResilienceOptions();

        options.RetryBaseDelay.ShouldBe(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryMaxDelay_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.RetryMaxDelay.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CircuitBreakerFailureRatio_Should_DefaultToFiftyPercent()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerFailureRatio.ShouldBe(0.5);
    }

    [Fact]
    public void CircuitBreakerSamplingDuration_Should_DefaultToSixtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerSamplingDuration.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void CircuitBreakerMinimumThroughput_Should_DefaultToTen()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerMinimumThroughput.ShouldBe(10);
    }

    [Fact]
    public void CircuitBreakerBreakDuration_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerBreakDuration.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void OperationTimeout_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.OperationTimeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Custom Values

    [Fact]
    public void MaxRetryAttempts_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { MaxRetryAttempts = 5 };

        options.MaxRetryAttempts.ShouldBe(5);
    }

    [Fact]
    public void MaxRetryAttempts_Should_AcceptZeroToDisableRetries()
    {
        var options = new SecretsResilienceOptions { MaxRetryAttempts = 0 };

        options.MaxRetryAttempts.ShouldBe(0);
    }

    [Fact]
    public void RetryBaseDelay_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { RetryBaseDelay = TimeSpan.FromSeconds(5) };

        options.RetryBaseDelay.ShouldBe(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryMaxDelay_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { RetryMaxDelay = TimeSpan.FromMinutes(1) };

        options.RetryMaxDelay.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CircuitBreakerFailureRatio_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { CircuitBreakerFailureRatio = 0.8 };

        options.CircuitBreakerFailureRatio.ShouldBe(0.8);
    }

    [Fact]
    public void CircuitBreakerSamplingDuration_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions
        {
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(120)
        };

        options.CircuitBreakerSamplingDuration.ShouldBe(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void CircuitBreakerMinimumThroughput_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { CircuitBreakerMinimumThroughput = 20 };

        options.CircuitBreakerMinimumThroughput.ShouldBe(20);
    }

    [Fact]
    public void CircuitBreakerBreakDuration_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions
        {
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };

        options.CircuitBreakerBreakDuration.ShouldBe(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void OperationTimeout_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { OperationTimeout = TimeSpan.FromSeconds(60) };

        options.OperationTimeout.ShouldBe(TimeSpan.FromSeconds(60));
    }

    #endregion
}
