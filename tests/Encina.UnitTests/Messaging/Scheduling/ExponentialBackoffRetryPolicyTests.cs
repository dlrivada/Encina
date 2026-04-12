using Encina.Messaging.Scheduling;

using Shouldly;

namespace Encina.UnitTests.Messaging.Scheduling;

/// <summary>
/// Unit tests for <see cref="ExponentialBackoffRetryPolicy"/>.
/// </summary>
public sealed class ExponentialBackoffRetryPolicyTests
{
    private static readonly DateTime Now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);

    #region RetryDecision Factory Helpers

    [Fact]
    public void RetryDecision_RetryAt_ProducesNonDeadLettered()
    {
        var nextUtc = Now.AddSeconds(5);
        var decision = RetryDecision.RetryAt(nextUtc);

        decision.NextRetryAtUtc.ShouldBe(nextUtc);
        decision.IsDeadLettered.ShouldBeFalse();
    }

    [Fact]
    public void RetryDecision_DeadLetter_ProducesNullNextRetry()
    {
        var decision = RetryDecision.DeadLetter();

        decision.NextRetryAtUtc.ShouldBeNull();
        decision.IsDeadLettered.ShouldBeTrue();
    }

    #endregion

    #region Compute — Exponential Delay

    [Fact]
    public void Compute_FirstRetry_ReturnsBaseDelay()
    {
        // Arrange: BaseRetryDelay = 5s, retryCount = 0 → delay = 5 * 2^0 = 5s
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5), MaxRetries = 3 };
        var sut = new ExponentialBackoffRetryPolicy(options);

        // Act
        var decision = sut.Compute(retryCount: 0, maxRetries: 3, nowUtc: Now);

        // Assert
        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldBe(Now.AddSeconds(5));
    }

    [Fact]
    public void Compute_SecondRetry_ReturnsDoubleDelay()
    {
        // retryCount = 1 → delay = 5 * 2^1 = 10s
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 1, maxRetries: 5, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldBe(Now.AddSeconds(10));
    }

    [Fact]
    public void Compute_ThirdRetry_ReturnsQuadrupleDelay()
    {
        // retryCount = 2 → delay = 5 * 2^2 = 20s
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 2, maxRetries: 5, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldBe(Now.AddSeconds(20));
    }

    [Fact]
    public void Compute_FourthRetry_ReturnsOctupleDelay()
    {
        // retryCount = 3 → delay = 5 * 2^3 = 40s
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 3, maxRetries: 5, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldBe(Now.AddSeconds(40));
    }

    #endregion

    #region Compute — Dead Letter

    [Fact]
    public void Compute_MaxRetriesExhausted_ReturnsDeadLetter()
    {
        // retryCount = 2, maxRetries = 3 → 2+1 >= 3 → dead letter
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 2, maxRetries: 3, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeTrue();
        decision.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Compute_RetryCountExceedsMaxRetries_ReturnsDeadLetter()
    {
        // retryCount = 5, maxRetries = 3 → 5+1 >= 3 → dead letter
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 5, maxRetries: 3, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeTrue();
        decision.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Compute_MaxRetriesOne_FirstFailureIsDeadLetter()
    {
        // retryCount = 0, maxRetries = 1 → 0+1 >= 1 → dead letter
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 0, maxRetries: 1, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeTrue();
    }

    #endregion

    #region Compute — Determinism

    [Fact]
    public void Compute_DeterministicForSameInputs()
    {
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(7) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var d1 = sut.Compute(retryCount: 1, maxRetries: 5, nowUtc: Now);
        var d2 = sut.Compute(retryCount: 1, maxRetries: 5, nowUtc: Now);

        d1.ShouldBe(d2);
    }

    #endregion

    #region Compute — Edge Cases

    [Fact]
    public void Compute_ZeroBaseDelay_RetryAtNow()
    {
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.Zero };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 0, maxRetries: 3, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldBe(Now); // 0 * 2^0 = 0
    }

    [Fact]
    public void Compute_MaxRetriesZero_AlwaysDeadLetters()
    {
        var options = new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5) };
        var sut = new ExponentialBackoffRetryPolicy(options);

        var decision = sut.Compute(retryCount: 0, maxRetries: 0, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeTrue();
    }

    #endregion
}
