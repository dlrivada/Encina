using Encina.Messaging.Outbox;
using Shouldly;

namespace Encina.GuardTests.Messaging.Outbox;

/// <summary>
/// Guard clause tests for <see cref="OutboxRetryBackoff.ComputeDelay"/> and the retry options it reads (#1150).
/// </summary>
public class OutboxRetryBackoffGuardTests
{
    private static readonly TimeSpan Base = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Max = TimeSpan.FromMinutes(10);

    [Fact]
    public void ComputeDelay_NegativeRetryCount_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutboxRetryBackoff.ComputeDelay(-1, Base, Max, 0, 0))
            .ParamName.ShouldBe("retryCount");
    }

    [Fact]
    public void ComputeDelay_NegativeBaseDelay_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutboxRetryBackoff.ComputeDelay(0, TimeSpan.FromTicks(-1), Max, 0, 0))
            .ParamName.ShouldBe("baseDelay");
    }

    [Fact]
    public void ComputeDelay_NegativeMaxDelay_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutboxRetryBackoff.ComputeDelay(0, Base, TimeSpan.FromTicks(-1), 0, 0))
            .ParamName.ShouldBe("maxDelay");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void ComputeDelay_JitterRatioOutsideZeroToOne_Throws(double jitterRatio)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutboxRetryBackoff.ComputeDelay(0, Base, Max, jitterRatio, 0))
            .ParamName.ShouldBe("jitterRatio");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.0)]
    [InlineData(double.NaN)]
    public void ComputeDelay_JitterSampleOutsideZeroToOne_Throws(double jitterSample)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => OutboxRetryBackoff.ComputeDelay(0, Base, Max, 0.2, jitterSample))
            .ParamName.ShouldBe("jitterSample");
    }

    [Fact]
    public void ComputeDelay_BoundaryValues_DoNotThrow()
    {
        OutboxRetryBackoff.ComputeDelay(0, TimeSpan.Zero, TimeSpan.Zero, 1, 0).ShouldBe(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(2.0)]
    public void OutboxOptions_RetryJitterRatioOutOfRange_Throws(double ratio)
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.RetryJitterRatio = ratio);
    }

    [Fact]
    public void OutboxOptions_NegativeMaxRetryDelay_Throws()
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.MaxRetryDelay = TimeSpan.FromTicks(-1));
    }

    [Fact]
    public void OutboxOptions_MaxRetriesBelowOne_Throws()
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.MaxRetries = 0);
    }

    [Fact]
    public void OutboxOptions_BatchSizeBelowOne_Throws()
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.BatchSize = 0);
    }

    [Fact]
    public void OutboxOptions_ZeroBaseRetryDelay_Throws()
    {
        var options = new OutboxOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.BaseRetryDelay = TimeSpan.Zero);
    }
}
