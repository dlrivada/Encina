using System.Net.Http.Headers;
using Encina.Recoverability;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Recoverability;

/// <summary>
/// Unit tests for <see cref="RetryAfterParser"/>, <see cref="OutboundRetryRules"/> and the outbound additions of
/// <see cref="DefaultErrorClassifier"/>.
/// </summary>
public sealed class OutboundRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _time = new(Now);
    private readonly DefaultErrorClassifier _classifier = new();

    #region Retry-After

    [Theory]
    [InlineData("0", 0)]
    [InlineData("2", 2)]
    [InlineData("120", 120)]
    public void Parse_DelaySeconds_ReturnsThatDelay(string value, int seconds) =>
        RetryAfterParser.Parse(value, _time).ShouldBe(TimeSpan.FromSeconds(seconds));

    [Fact]
    public void Parse_FutureHttpDate_ReturnsTheRemainingDelay()
    {
        var value = Now.AddSeconds(90).ToString("R");

        RetryAfterParser.Parse(value, _time).ShouldBe(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void Parse_PastHttpDate_ReturnsZero() =>
        RetryAfterParser.Parse(Now.AddHours(-1).ToString("R"), _time).ShouldBe(TimeSpan.Zero);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("soon")]
    [InlineData("-5")]
    public void Parse_MissingOrInvalid_ReturnsNone(string? value) =>
        RetryAfterParser.Parse(value, _time).ShouldBeNull();

    [Fact]
    public void Parse_ParsedHeaderValue_UsesDeltaThenDate()
    {
        RetryAfterParser.Parse(new RetryConditionHeaderValue(TimeSpan.FromSeconds(7)), _time).ShouldBe(TimeSpan.FromSeconds(7));
        RetryAfterParser.Parse(new RetryConditionHeaderValue(Now.AddSeconds(3)), _time).ShouldBe(TimeSpan.FromSeconds(3));
        RetryAfterParser.Parse((RetryConditionHeaderValue?)null, _time).ShouldBeNull();
    }

    #endregion

    #region OutboundRetryRules

    [Fact]
    public void AllowsAutomaticRetry_AmbiguousError_RequiresAnOptIn()
    {
        var ambiguous = OutboundTestSupport.Outbound(OutboundErrorCodes.ServerError, transient: true, status: 502, ambiguous: true);

        OutboundRetryRules.AllowsAutomaticRetry(ambiguous, null, false).ShouldBeFalse();
        OutboundRetryRules.AllowsAutomaticRetry(ambiguous, "", false).ShouldBeFalse();
        OutboundRetryRules.AllowsAutomaticRetry(ambiguous, "key", false).ShouldBeTrue();
        OutboundRetryRules.AllowsAutomaticRetry(ambiguous, null, true).ShouldBeTrue();
    }

    [Fact]
    public void AllowsAutomaticRetry_ErrorWithoutAmbiguity_IsAlwaysAllowed()
    {
        OutboundRetryRules.AllowsAutomaticRetry(
            OutboundTestSupport.Outbound(OutboundErrorCodes.RateLimited, transient: true, status: 429), null, false).ShouldBeTrue();
        OutboundRetryRules.AllowsAutomaticRetry(EncinaError.New("plain"), null, false).ShouldBeTrue();
    }

    [Fact]
    public void GetRetryAfter_ReadsOnlyATimeSpanMetadataValue()
    {
        OutboundRetryRules.GetRetryAfter(
            OutboundTestSupport.Outbound(OutboundErrorCodes.RateLimited, true, 429, TimeSpan.FromSeconds(4))).ShouldBe(TimeSpan.FromSeconds(4));
        OutboundRetryRules.GetRetryAfter(OutboundTestSupport.Outbound(OutboundErrorCodes.ServerError, true, 503)).ShouldBeNull();
        OutboundRetryRules.GetRetryAfter(EncinaError.New("plain")).ShouldBeNull();
        OutboundRetryRules.GetRetryAfter(EncinaErrors.Create(
            "x", "y", details: new Dictionary<string, object?> { [OutboundErrorMetadataKeys.HttpRetryAfter] = "4" })).ShouldBeNull();
    }

    #endregion

    #region Classifier

    [Theory]
    [InlineData(true, ErrorClassification.Transient)]
    [InlineData(false, ErrorClassification.Permanent)]
    public void Classify_DeclaredTransientMetadata_WinsOverInference(bool transient, ErrorClassification expected)
    {
        // The code and the cause would classify the opposite way
        var details = new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = transient };
        var cause = transient ? new ArgumentException("x") : (Exception)new TimeoutException();
        var error = EncinaErrors.Create("encina.guard.validation_failed", "m", cause, details);

        _classifier.Classify(error, cause).ShouldBe(expected);
    }

    [Fact]
    public void Classify_NonBooleanTransientMetadata_IsIgnored()
    {
        var error = EncinaErrors.Create("x", "m", details: new Dictionary<string, object?> { [OutboundErrorMetadataKeys.Transient] = "yes" });

        _classifier.Classify(error, null).ShouldBe(ErrorClassification.Unknown);
    }

    [Theory]
    [InlineData("Timeout expired.  The timeout period elapsed prior to obtaining a connection from the pool.  This may have occurred because all pooled connections were in use and max pool size was reached.")]
    [InlineData("The connection pool has been exhausted, either raise 'Max Pool Size' (currently 1) or 'Timeout' (currently 1 seconds) in your connection string.")]
    [InlineData("Connect Timeout expired. All pooled connections are in use.")]
    public void Classify_PoolExhaustionAnywhereInTheChain_IsTransient(string driverMessage)
    {
        var pool = new InvalidOperationException(driverMessage);
        var wrapped = new IOException("outer", new ArgumentNullException("middle", pool));

        _classifier.Classify(EncinaError.New("failed"), pool).ShouldBe(ErrorClassification.Transient);
        _classifier.Classify(EncinaError.New("failed"), wrapped).ShouldBe(ErrorClassification.Transient);
        _classifier.Classify(EncinaErrors.Create("db.failed", "m", pool), null).ShouldBe(ErrorClassification.Transient);
    }

    [Fact]
    public void Classify_OtherInvalidOperation_StaysPermanent() =>
        _classifier.Classify(EncinaError.New("failed"), new InvalidOperationException("Sequence contains no elements"))
            .ShouldBe(ErrorClassification.Permanent);

    [Fact]
    public void Classify_Http408_IsTransient() =>
        _classifier.Classify(EncinaError.New("failed"), new HttpRequestException("x", null, System.Net.HttpStatusCode.RequestTimeout))
            .ShouldBe(ErrorClassification.Transient);

    #endregion

    #region Metrics

    [Fact]
    public void RecordSkippedAndAttempt_EmitTheCountersWithBoundedTags()
    {
        using var capture = new MetricCapture("encina.retry.skipped_total", "encina.retry.attempts_total");

        RetryMetrics.RecordSkipped(RetryMetrics.ReasonPermanent, "ReqSkip", "code.a", "t-1");
        RetryMetrics.RecordAttempt("ReqAttempt", "code.b", null);

        var skipped = capture.Tags("encina.retry.skipped_total", "ReqSkip").ShouldHaveSingleItem();
        skipped["reason"].ShouldBe("permanent");
        skipped["error_code"].ShouldBe("code.a");
        skipped["encina.tenant_id"].ShouldBe("t-1");
        var attempt = capture.Tags("encina.retry.attempts_total", "ReqAttempt").ShouldHaveSingleItem();
        attempt.ContainsKey("encina.tenant_id").ShouldBeFalse();
    }

    #endregion
}
