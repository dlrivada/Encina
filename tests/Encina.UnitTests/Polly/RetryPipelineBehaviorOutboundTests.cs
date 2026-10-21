using Encina.Polly;
using Encina.Recoverability;
using Encina.UnitTests.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Encina.UnitTests.Polly;

/// <summary>
/// Retry behavior against the outbound error taxonomy (#1223): permanent errors, Retry-After, non-idempotent sends,
/// classification of exceptions and telemetry.
/// </summary>
public sealed class RetryPipelineBehaviorOutboundTests
{
    private static readonly TimeSpan Step = TimeSpan.FromMilliseconds(100);

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record Standard : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record PermanentReq : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record ShortWait : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record LongWait : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record Matrix : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000, RetryNonIdempotent = true)]
    public sealed record MatrixOptIn : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record TenantA : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record TenantB : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000)]
    public sealed record Instrumented : IRequest<string>;

    [Retry(MaxAttempts = 3, BaseDelayMs = 10, MaxDelayMs = 5000, RetryOnAllExceptions = true)]
    public sealed record AllExceptions : IRequest<string>;

    private static RetryPipelineBehavior<TRequest, string> Create<TRequest>(TimeProvider? time = null)
        where TRequest : IRequest<string> =>
        new(NullLogger<RetryPipelineBehavior<TRequest, string>>.Instance, new DefaultErrorClassifier(), time);

    private static IRequestContext Context(string? tenantId = null, string? idempotencyKey = null)
    {
        var context = Substitute.For<IRequestContext>();
        context.TenantId.Returns(tenantId);
        context.IdempotencyKey.Returns(idempotencyKey);
        return context;
    }

    private static async Task<T> AdvanceUntilDone<T>(FakeTimeProvider time, Task<T> task)
    {
        for (var i = 0; i < 2000 && !task.IsCompleted; i++)
        {
            time.Advance(Step);
            await Task.Delay(1);
        }

        return await task;
    }

    [Fact]
    public async Task Handle_ClientError400_IsAttemptedOnceAndCountedAsPermanentSkip()
    {
        // Arrange
        using var metrics = new MetricCapture("encina.retry.skipped_total");
        var behavior = Create<PermanentReq>();
        var calls = 0;
        RequestHandlerCallback<string> next = () =>
        {
            calls++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(
                OutboundTestSupport.Outbound(OutboundErrorCodes.ClientError, transient: false, status: 400)));
        };

        // Act
        var result = await behavior.Handle(new PermanentReq(), Context(), next, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        calls.ShouldBe(1);
        var skipped = metrics.Tags("encina.retry.skipped_total", nameof(PermanentReq));
        skipped.Count.ShouldBe(1);
        skipped[0]["reason"].ShouldBe(RetryMetrics.ReasonPermanent);
    }

    [Fact]
    public async Task Handle_RateLimitedWithRetryAfterWithinMax_RetriesAfterExactlyTheHeaderDelay()
    {
        // Arrange
        var time = new FakeTimeProvider();
        var behavior = Create<ShortWait>(time);
        var calls = new List<DateTimeOffset>();
        RequestHandlerCallback<string> next = () =>
        {
            calls.Add(time.GetUtcNow());
            return calls.Count == 1
                ? ValueTask.FromResult(Prelude.Left<EncinaError, string>(OutboundTestSupport.Outbound(
                    OutboundErrorCodes.RateLimited, transient: true, status: 429, retryAfter: TimeSpan.FromSeconds(2))))
                : ValueTask.FromResult(Prelude.Right<EncinaError, string>("ok"));
        };

        // Act
        var task = behavior.Handle(new ShortWait(), Context(), next, CancellationToken.None).AsTask();
        time.Advance(TimeSpan.FromMilliseconds(1900));
        await Task.Delay(50);
        var callsBefore = calls.Count;
        var result = await AdvanceUntilDone(time, task);

        // Assert
        callsBefore.ShouldBe(1, "the retry must not happen earlier than Retry-After");
        result.IsRight.ShouldBeTrue();
        calls.Count.ShouldBe(2);
        (calls[1] - calls[0]).ShouldBe(TimeSpan.FromSeconds(2), "the wait is exactly the header delay");
    }

    [Fact]
    public async Task Handle_RetryAfterBeyondMaxDelay_ReturnsTheTransientErrorWithMetadataAndDoesNotRetry()
    {
        // Arrange
        using var metrics = new MetricCapture("encina.retry.skipped_total");
        var behavior = Create<LongWait>();
        var calls = 0;
        RequestHandlerCallback<string> next = () =>
        {
            calls++;
            return ValueTask.FromResult(Prelude.Left<EncinaError, string>(OutboundTestSupport.Outbound(
                OutboundErrorCodes.RateLimited, transient: true, status: 429, retryAfter: TimeSpan.FromSeconds(120))));
        };

        // Act
        var result = await behavior.Handle(new LongWait(), Context(), next, CancellationToken.None);

        // Assert
        calls.ShouldBe(1);
        var error = (EncinaError)result;
        OutboundRetryRules.GetRetryAfter(error).ShouldBe(TimeSpan.FromSeconds(120));
        error.GetMetadata()[OutboundErrorMetadataKeys.Transient].ShouldBe(true);
        metrics.Tags("encina.retry.skipped_total", nameof(LongWait))[0]["reason"]
            .ShouldBe(RetryMetrics.ReasonRetryAfterExceedsMax);
    }

    [Theory]
    [InlineData(true, null, false, 1)]
    [InlineData(true, "key-1", false, 2)]
    [InlineData(true, null, true, 2)]
    [InlineData(false, null, false, 2)]
    public async Task Handle_FailureOnNonIdempotentSend_IsRetriedOnlyWithAnOptIn(bool ambiguous, string? key, bool optIn, int expectedCalls)
    {
        // Arrange: a 502 on a POST is transient and ambiguous; a 503 or a 429 is not ambiguous
        var calls = 0;
        RequestHandlerCallback<string> next = () =>
        {
            calls++;
            return calls == 1
                ? ValueTask.FromResult(Prelude.Left<EncinaError, string>(OutboundTestSupport.Outbound(
                    OutboundErrorCodes.ServerError, transient: true, status: ambiguous ? 502 : 503, ambiguous: ambiguous)))
                : ValueTask.FromResult(Prelude.Right<EncinaError, string>("ok"));
        };

        // Act
        var result = optIn
            ? await Create<MatrixOptIn>().Handle(new MatrixOptIn(), Context(idempotencyKey: key), next, CancellationToken.None)
            : await Create<Matrix>().Handle(new Matrix(), Context(idempotencyKey: key), next, CancellationToken.None);

        // Assert
        calls.ShouldBe(expectedCalls);
        result.IsRight.ShouldBe(expectedCalls == 2);
    }

    [Fact]
    public async Task Handle_AmbiguousFailureWithoutOptIn_IsCountedAsNonIdempotentSkip()
    {
        // Arrange
        using var metrics = new MetricCapture("encina.retry.skipped_total");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Prelude.Left<EncinaError, string>(
            OutboundTestSupport.Outbound(OutboundErrorCodes.ServerError, transient: true, status: 502, ambiguous: true)));

        // Act
        await Create<Standard>().Handle(new Standard(), Context(), next, CancellationToken.None);

        // Assert
        metrics.Tags("encina.retry.skipped_total", nameof(Standard))[0]["reason"].ShouldBe(RetryMetrics.ReasonNonIdempotent);
    }

    [Fact]
    public async Task Handle_PermanentExceptionWithRetryOnAllExceptions_IsNotRetriedButUnknownIs()
    {
        // Arrange
        var permanentCalls = 0;
        var unknownCalls = 0;
        RequestHandlerCallback<string> permanent = () => { permanentCalls++; throw new ArgumentException("bad"); };
        RequestHandlerCallback<string> unknown = () => { unknownCalls++; throw new UnknownFailure(); };

        // Act
        await Create<AllExceptions>().Handle(new AllExceptions(), Context(), permanent, CancellationToken.None);
        await Create<AllExceptions>().Handle(new AllExceptions(), Context(), unknown, CancellationToken.None);

        // Assert
        permanentCalls.ShouldBe(1);
        unknownCalls.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_FailedAttempts_RecordActivityAndCountersWithOnlyBoundedTags()
    {
        // Arrange
        using var metrics = new MetricCapture("encina.retry.attempts_total");
        using var activities = new ActivityCapture("Encina.Retry");
        const string Sentinel = "SENTINEL-BODY-DATA-SUBJECT";
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Prelude.Left<EncinaError, string>(
            EncinaErrors.Create(OutboundErrorCodes.ServerError, Sentinel, details: new Dictionary<string, object?>
            {
                [OutboundErrorMetadataKeys.Transient] = true
            })));

        // Act
        await Create<Instrumented>().Handle(new Instrumented(), Context("tenant-x"), next, CancellationToken.None);

        // Assert
        var attempts = metrics.Tags("encina.retry.attempts_total", nameof(Instrumented));
        attempts.Count.ShouldBe(2);
        attempts.ShouldAllBe(tags => (string?)tags["encina.tenant_id"] == "tenant-x");
        var activity = activities.Activities(nameof(Instrumented)).ShouldHaveSingleItem();
        activity.GetTagItem("encina.tenant_id").ShouldBe("tenant-x");
        activity.GetTagItem("Encina.failure_code").ShouldBe(OutboundErrorCodes.ServerError);
        attempts.SelectMany(tags => tags.Values).Select(v => v?.ToString()).ShouldNotContain(Sentinel);
        activity.TagObjects.Select(t => t.Value?.ToString()).ShouldNotContain(Sentinel);
        activity.StatusDescription.ShouldBe(OutboundErrorCodes.ServerError);
    }

    [Fact]
    public async Task Handle_TwoTenants_EachCallsTelemetryCarriesOnlyItsOwnTenant()
    {
        // Arrange
        using var metrics = new MetricCapture("encina.retry.attempts_total");
        using var activities = new ActivityCapture("Encina.Retry");
        RequestHandlerCallback<string> next = () => ValueTask.FromResult(Prelude.Left<EncinaError, string>(
            OutboundTestSupport.Outbound(OutboundErrorCodes.ServerError, transient: true, status: 503)));

        // Act
        await Task.WhenAll(
            Create<TenantA>().Handle(new TenantA(), Context("tenant-a"), next, CancellationToken.None).AsTask(),
            Create<TenantB>().Handle(new TenantB(), Context("tenant-b"), next, CancellationToken.None).AsTask());

        // Assert
        metrics.Tags("encina.retry.attempts_total", nameof(TenantA)).ShouldAllBe(t => (string?)t["encina.tenant_id"] == "tenant-a");
        metrics.Tags("encina.retry.attempts_total", nameof(TenantB)).ShouldAllBe(t => (string?)t["encina.tenant_id"] == "tenant-b");
        activities.Activities(nameof(TenantA)).Single().GetTagItem("encina.tenant_id").ShouldBe("tenant-a");
        activities.Activities(nameof(TenantB)).Single().GetTagItem("encina.tenant_id").ShouldBe("tenant-b");
    }

    private sealed class UnknownFailure : Exception;
}
