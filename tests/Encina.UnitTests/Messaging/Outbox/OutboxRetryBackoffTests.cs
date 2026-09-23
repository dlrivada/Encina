using Encina.Messaging.Outbox;

namespace Encina.UnitTests.Messaging.Outbox;

/// <summary>
/// Unit tests for <see cref="OutboxRetryBackoff"/> (#1150).
/// </summary>
public sealed class OutboxRetryBackoffTests
{
    private static readonly TimeSpan Base = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(10);

    [Theory]
    [InlineData(0, 5)]
    [InlineData(1, 10)]
    [InlineData(2, 20)]
    [InlineData(5, 160)]
    public void ComputeDelay_WithoutJitter_DoublesPerRetry(int retryCount, int expectedSeconds)
    {
        OutboxRetryBackoff.ComputeDelay(retryCount, Base, Max, jitterRatio: 0, jitterSample: 0.9)
            .ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(int.MaxValue)]
    public void ComputeDelay_LargeRetryCount_IsCappedByMaxDelay(int retryCount)
    {
        OutboxRetryBackoff.ComputeDelay(retryCount, Base, Max, jitterRatio: 0, jitterSample: 0)
            .ShouldBe(Max);
    }

    [Fact]
    public void ComputeDelay_MaxDelayBelowBaseDelay_ReturnsMaxDelay()
    {
        OutboxRetryBackoff.ComputeDelay(0, Base, TimeSpan.FromSeconds(1), jitterRatio: 0, jitterSample: 0)
            .ShouldBe(TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData(0.2, 0.0, 10)]
    [InlineData(0.2, 0.5, 9)]
    [InlineData(1.0, 0.5, 5)]
    public void ComputeDelay_WithJitter_ReducesTheDelayByRatioTimesSample(double ratio, double sample, double expectedSeconds)
    {
        OutboxRetryBackoff.ComputeDelay(1, Base, Max, ratio, sample)
            .ShouldBe(TimeSpan.FromSeconds(expectedSeconds));
    }

    [Fact]
    public void ComputeDelay_ZeroBaseDelay_ReturnsZero()
    {
        OutboxRetryBackoff.ComputeDelay(int.MaxValue, TimeSpan.Zero, Max, jitterRatio: 0.2, jitterSample: 0.5)
            .ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void ComputeDelay_ZeroMaxDelay_ReturnsZero()
    {
        OutboxRetryBackoff.ComputeDelay(3, Base, TimeSpan.Zero, jitterRatio: 0, jitterSample: 0)
            .ShouldBe(TimeSpan.Zero);
    }

    [Fact]
    public void OutboxOptions_Defaults_AreTenMinuteCapAndTwentyPercentJitter()
    {
        var options = new OutboxOptions();

        options.MaxRetryDelay.ShouldBe(TimeSpan.FromMinutes(10));
        options.RetryJitterRatio.ShouldBe(0.2);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void OutboxOptions_RetryJitterRatio_AcceptsValuesBetweenZeroAndOne(double ratio)
    {
        var options = new OutboxOptions { RetryJitterRatio = ratio };

        options.RetryJitterRatio.ShouldBe(ratio);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void OutboxOptions_RetryJitterRatio_RejectsValuesOutsideZeroToOne(double ratio)
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.RetryJitterRatio = ratio);
    }

    [Fact]
    public void OutboxOptions_MaxRetryDelay_RejectsNegativeValues()
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.MaxRetryDelay = TimeSpan.FromSeconds(-1));
    }
}
