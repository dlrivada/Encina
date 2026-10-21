using System.Net.Http.Headers;
using Encina.Recoverability;

namespace Encina.GuardTests.Recoverability;

/// <summary>
/// Guard clause tests for the public methods of the outbound taxonomy in core (#1223).
/// </summary>
public sealed class OutboundGuardTests
{
    [Fact]
    public void RetryAfterParser_ParseHeader_NullTimeProvider_Throws() =>
        Should.Throw<ArgumentNullException>(() => RetryAfterParser.Parse(new RetryConditionHeaderValue(TimeSpan.FromSeconds(1)), null!))
            .ParamName.ShouldBe("timeProvider");

    [Fact]
    public void RetryAfterParser_ParseText_NullTimeProvider_Throws() =>
        Should.Throw<ArgumentNullException>(() => RetryAfterParser.Parse("1", null!))
            .ParamName.ShouldBe("timeProvider");

    [Fact]
    public void RetryMetrics_RecordAttempt_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => RetryMetrics.RecordAttempt(null!, "code", null)).ParamName.ShouldBe("requestName");
        Should.Throw<ArgumentNullException>(() => RetryMetrics.RecordAttempt("req", null!, null)).ParamName.ShouldBe("errorCode");
    }

    [Fact]
    public void RetryMetrics_RecordSkipped_NullArguments_Throw()
    {
        Should.Throw<ArgumentNullException>(() => RetryMetrics.RecordSkipped(null!, "req", "code", null)).ParamName.ShouldBe("reason");
        Should.Throw<ArgumentNullException>(() => RetryMetrics.RecordSkipped("r", null!, "code", null)).ParamName.ShouldBe("requestName");
        Should.Throw<ArgumentNullException>(() => RetryMetrics.RecordSkipped("r", "req", null!, null)).ParamName.ShouldBe("errorCode");
    }

    [Fact]
    public void OutboundRetryRules_NullIdempotencyKeyAndDefaultError_DoNotThrow()
    {
        OutboundRetryRules.AllowsAutomaticRetry(default, null, false).ShouldBeTrue();
        OutboundRetryRules.GetRetryAfter(default).ShouldBeNull();
    }

    [Fact]
    public void DefaultErrorClassifier_Classify_DefaultErrorAndNullException_DoesNotThrow() =>
        new DefaultErrorClassifier().Classify(default, null).ShouldBe(ErrorClassification.Unknown);
}
