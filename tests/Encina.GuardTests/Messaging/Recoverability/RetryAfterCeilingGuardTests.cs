using Encina.Messaging.Outbox;
using Encina.Messaging.Recoverability;

namespace Encina.GuardTests.Messaging.Recoverability;

/// <summary>
/// Guards of the Retry-After ceiling settings (#2588): zero and negative values are rejected, a positive value is kept.
/// </summary>
public sealed class RetryAfterCeilingGuardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void RecoverabilityOptions_MaxRetryAfter_RejectsZeroAndNegative(int seconds)
    {
        var options = new RecoverabilityOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.MaxRetryAfter = TimeSpan.FromSeconds(seconds));
        options.MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void OutboxOptions_MaxRetryAfter_RejectsZeroAndNegative(int seconds)
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.MaxRetryAfter = TimeSpan.FromSeconds(seconds));
        options.MaxRetryAfter.ShouldBe(TimeSpan.FromHours(24));
    }

    [Fact]
    public void MaxRetryAfter_PositiveValue_IsKept()
    {
        new RecoverabilityOptions { MaxRetryAfter = TimeSpan.FromMinutes(1) }.MaxRetryAfter.ShouldBe(TimeSpan.FromMinutes(1));
        new OutboxOptions { MaxRetryAfter = TimeSpan.FromMinutes(1) }.MaxRetryAfter.ShouldBe(TimeSpan.FromMinutes(1));
    }
}
