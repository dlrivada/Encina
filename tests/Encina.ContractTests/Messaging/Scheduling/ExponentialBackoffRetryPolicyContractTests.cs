using Encina.Messaging.Scheduling;

using Shouldly;

namespace Encina.ContractTests.Messaging.Scheduling;

/// <summary>
/// Contract tests verifying that <see cref="ExponentialBackoffRetryPolicy"/> satisfies
/// the <see cref="IScheduledMessageRetryPolicy"/> contract invariants.
/// </summary>
public sealed class ExponentialBackoffRetryPolicyContractTests
{
    private static readonly DateTime Now = new(2026, 4, 12, 10, 0, 0, DateTimeKind.Utc);

    private static ExponentialBackoffRetryPolicy CreateSut(int maxRetries = 3) =>
        new(new SchedulingOptions { BaseRetryDelay = TimeSpan.FromSeconds(5), MaxRetries = maxRetries });

    [Fact]
    public void Compute_WhenNotDeadLettered_NextRetryAtUtcIsNotNull()
    {
        var sut = CreateSut(maxRetries: 5);
        var decision = sut.Compute(retryCount: 0, maxRetries: 5, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeFalse();
        decision.NextRetryAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Compute_WhenDeadLettered_NextRetryAtUtcIsNull()
    {
        var sut = CreateSut(maxRetries: 1);
        var decision = sut.Compute(retryCount: 0, maxRetries: 1, nowUtc: Now);

        decision.IsDeadLettered.ShouldBeTrue();
        decision.NextRetryAtUtc.ShouldBeNull();
    }

    [Fact]
    public void Compute_WhenNotDeadLettered_NextRetryIsInTheFuture()
    {
        var sut = CreateSut(maxRetries: 5);
        var decision = sut.Compute(retryCount: 0, maxRetries: 5, nowUtc: Now);

        decision.NextRetryAtUtc.ShouldNotBeNull();
        decision.NextRetryAtUtc!.Value.ShouldBeGreaterThanOrEqualTo(Now);
    }

    [Fact]
    public void Compute_SuccessiveRetries_DelayIncreases()
    {
        var sut = CreateSut(maxRetries: 10);

        var d0 = sut.Compute(retryCount: 0, maxRetries: 10, nowUtc: Now);
        var d1 = sut.Compute(retryCount: 1, maxRetries: 10, nowUtc: Now);
        var d2 = sut.Compute(retryCount: 2, maxRetries: 10, nowUtc: Now);

        d1.NextRetryAtUtc!.Value.ShouldBeGreaterThan(d0.NextRetryAtUtc!.Value);
        d2.NextRetryAtUtc!.Value.ShouldBeGreaterThan(d1.NextRetryAtUtc!.Value);
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var sut = CreateSut(maxRetries: 5);

        var first = sut.Compute(retryCount: 1, maxRetries: 5, nowUtc: Now);
        var second = sut.Compute(retryCount: 1, maxRetries: 5, nowUtc: Now);

        first.ShouldBe(second);
    }
}
