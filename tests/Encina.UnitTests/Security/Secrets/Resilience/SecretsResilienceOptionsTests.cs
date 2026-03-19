using Encina.Security.Secrets.Resilience;
using FluentAssertions;

namespace Encina.UnitTests.Security.Secrets.Resilience;

public sealed class SecretsResilienceOptionsTests
{
    #region Default Values

    [Fact]
    public void MaxRetryAttempts_Should_DefaultToThree()
    {
        var options = new SecretsResilienceOptions();

        options.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public void RetryBaseDelay_Should_DefaultToTwoSeconds()
    {
        var options = new SecretsResilienceOptions();

        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryMaxDelay_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.RetryMaxDelay.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void CircuitBreakerFailureRatio_Should_DefaultToFiftyPercent()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerFailureRatio.Should().Be(0.5);
    }

    [Fact]
    public void CircuitBreakerSamplingDuration_Should_DefaultToSixtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerSamplingDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void CircuitBreakerMinimumThroughput_Should_DefaultToTen()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerMinimumThroughput.Should().Be(10);
    }

    [Fact]
    public void CircuitBreakerBreakDuration_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void OperationTimeout_Should_DefaultToThirtySeconds()
    {
        var options = new SecretsResilienceOptions();

        options.OperationTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    #endregion

    #region Custom Values

    [Fact]
    public void MaxRetryAttempts_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { MaxRetryAttempts = 5 };

        options.MaxRetryAttempts.Should().Be(5);
    }

    [Fact]
    public void MaxRetryAttempts_Should_AcceptZeroToDisableRetries()
    {
        var options = new SecretsResilienceOptions { MaxRetryAttempts = 0 };

        options.MaxRetryAttempts.Should().Be(0);
    }

    [Fact]
    public void RetryBaseDelay_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { RetryBaseDelay = TimeSpan.FromSeconds(5) };

        options.RetryBaseDelay.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RetryMaxDelay_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { RetryMaxDelay = TimeSpan.FromMinutes(1) };

        options.RetryMaxDelay.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void CircuitBreakerFailureRatio_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { CircuitBreakerFailureRatio = 0.8 };

        options.CircuitBreakerFailureRatio.Should().Be(0.8);
    }

    [Fact]
    public void CircuitBreakerSamplingDuration_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions
        {
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(120)
        };

        options.CircuitBreakerSamplingDuration.Should().Be(TimeSpan.FromSeconds(120));
    }

    [Fact]
    public void CircuitBreakerMinimumThroughput_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { CircuitBreakerMinimumThroughput = 20 };

        options.CircuitBreakerMinimumThroughput.Should().Be(20);
    }

    [Fact]
    public void CircuitBreakerBreakDuration_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions
        {
            CircuitBreakerBreakDuration = TimeSpan.FromMinutes(1)
        };

        options.CircuitBreakerBreakDuration.Should().Be(TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void OperationTimeout_Should_AcceptCustomValue()
    {
        var options = new SecretsResilienceOptions { OperationTimeout = TimeSpan.FromSeconds(60) };

        options.OperationTimeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    #endregion
}
