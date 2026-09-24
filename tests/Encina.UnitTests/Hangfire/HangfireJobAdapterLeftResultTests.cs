using Encina.Hangfire;
using Encina.Messaging.Recoverability;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Hangfire;

/// <summary>
/// Failure semantics of the Hangfire job adapters (#1152, #1159 review): a handler <c>Left</c>
/// must throw so Hangfire records the job as failed. Cancellation surfaces as
/// <see cref="OperationCanceledException"/>, permanent failures as
/// <see cref="EncinaJobPermanentFailureException"/>, and everything else as
/// <see cref="EncinaJobFailedException"/>. The exception never carries <see cref="EncinaError.Message"/>,
/// because Hangfire persists exception messages and the error message may contain personal data.
/// </summary>
public sealed class HangfireJobAdapterLeftResultTests
{
    private const string SensitiveMessage = "Consent missing for subject 'patient-7f3a'";

    // ─── Request adapter ───

    [Fact]
    public async Task ExecuteAsync_TransientLeft_ThrowsEncinaJobFailedException()
    {
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("store.timeout", "Store timed out")));

        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        exception.ErrorCode.ShouldBe("store.timeout");
    }

    [Fact]
    public async Task ExecuteAsync_UnclassifiedLeft_IsTreatedAsTransient()
    {
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("test.error", "Handler rejected the request")));

        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        exception.ErrorCode.ShouldBe("test.error");
    }

    [Theory]
    [InlineData("Encina.guard.validation_failed")]
    [InlineData("consent.missing")]
    [InlineData("dsr.restriction_active")]
    [InlineData("encina.request.handler_missing")]
    public async Task ExecuteAsync_PermanentLeft_ThrowsEncinaJobPermanentFailureException(string code)
    {
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(EncinaErrors.Create(code, SensitiveMessage)));

        var exception = await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        exception.ErrorCode.ShouldBe(code);
    }

    [Fact]
    public async Task ExecuteAsync_Left_ExceptionDoesNotLeakTheErrorMessage()
    {
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("consent.missing", SensitiveMessage)));

        var exception = await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        exception.Message.ShouldContain("consent.missing");
        exception.Message.ShouldNotContain("patient-7f3a");
        exception.ToString().ShouldNotContain("patient-7f3a");
        exception.Data.Count.ShouldBe(1);
        exception.Data[EncinaJobPermanentFailureException.ErrorCodeDataKey].ShouldBe("consent.missing");
    }

    [Fact]
    public async Task ExecuteAsync_LeftWithException_PassesOnlyTheCauseTypeAsInnerException()
    {
        var cause = new TimeoutException("db timeout for patient-9");
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("store.failure", "Store failed", cause)));

        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        // Hangfire persists the full exception chain, so the cause itself (and its Message,
        // which may carry personal data) never becomes the InnerException (#1259 review).
        exception.InnerException.ShouldNotBeSameAs(cause);
        exception.InnerException!.Message.ShouldContain(nameof(TimeoutException));
        exception.ToString().ShouldNotContain("db timeout for patient-9");
    }

    [Fact]
    public async Task ExecuteAsync_CancelledLeft_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var cause = new OperationCanceledException(cts.Token);
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(
            EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, "The SpikeRequest request was cancelled.", cause)));

        var exception = await Should.ThrowAsync<OperationCanceledException>(() => adapter.ExecuteAsync(new SpikeRequest("payload"), cts.Token));

        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledLeft_WithoutCancelledToken_IsAFailure()
    {
        var adapter = CreateRequestAdapter(Left<EncinaError, SpikeResponse>(
            EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, "cancelled by the handler")));

        await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.ExecuteAsync(new SpikeRequest("payload"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_UsesTheInjectedErrorClassifier()
    {
        var classifier = Substitute.For<IErrorClassifier>();
        classifier.Classify(Arg.Any<EncinaError>(), Arg.Any<Exception?>()).Returns(ErrorClassification.Permanent);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("custom.domain_rule", "rule broken")));
        var adapter = new HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>(
            encina, NullLogger<HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>>.Instance, classifier);

        await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        classifier.Received(1).Classify(Arg.Any<EncinaError>(), Arg.Any<Exception?>());
    }

    [Fact]
    public async Task ExecuteAsync_PassesTheCauseToTheErrorClassifier()
    {
        // A custom classifier keyed on the exception type sees the handler's exception.
        var cause = new PoisonMessageException();
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("custom.failure", "failed", cause)));
        var adapter = new HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>(
            encina, NullLogger<HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>>.Instance, new PoisonMessageClassifier());

        var exception = await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        // The classifier still sees the real cause (asserted above); the exception Hangfire
        // persists carries only its type, never the instance itself (#1259 review).
        exception.InnerException.ShouldNotBeSameAs(cause);
        exception.InnerException!.Message.ShouldContain(nameof(PoisonMessageException));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutCause_PassesNoExceptionToTheErrorClassifier()
    {
        var classifier = Substitute.For<IErrorClassifier>();
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("custom.failure", "failed")));
        var adapter = new HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>(
            encina, NullLogger<HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>>.Instance, classifier);

        await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.ExecuteAsync(new SpikeRequest("payload")));

        classifier.Received(1).Classify(Arg.Any<EncinaError>(), null);
    }

    [Fact]
    public async Task PublishAsync_PassesTheCauseToTheErrorClassifier()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<SpikeNotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(
                EncinaErrors.Create("custom.failure", "failed", new PoisonMessageException()))));
        var adapter = new HangfireNotificationJobAdapter<SpikeNotification>(
            encina, NullLogger<HangfireNotificationJobAdapter<SpikeNotification>>.Instance, new PoisonMessageClassifier());

        await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.PublishAsync(new SpikeNotification("payload")));
    }

    [Fact]
    public async Task ExecuteAsync_Right_ReturnsTheResponse()
    {
        var response = new SpikeResponse("ok");
        var adapter = CreateRequestAdapter(Right<EncinaError, SpikeResponse>(response));

        var result = await adapter.ExecuteAndReturnResultAsync(new SpikeRequest("payload"));

        result.ShouldBe(response);
    }

    // ─── Notification adapter ───

    [Fact]
    public async Task PublishAsync_TransientLeft_ThrowsEncinaJobFailedException()
    {
        var adapter = CreateNotificationAdapter(EncinaErrors.Create("test.error", "Handler rejected the notification"));

        var exception = await Should.ThrowAsync<EncinaJobFailedException>(() => adapter.PublishAsync(new SpikeNotification("payload")));

        exception.ErrorCode.ShouldBe("test.error");
    }

    [Fact]
    public async Task PublishAsync_PermanentLeft_ThrowsEncinaJobPermanentFailureException()
    {
        var adapter = CreateNotificationAdapter(EncinaErrors.Create("consent.withdrawn", SensitiveMessage));

        var exception = await Should.ThrowAsync<EncinaJobPermanentFailureException>(() => adapter.PublishAsync(new SpikeNotification("payload")));

        exception.ErrorCode.ShouldBe("consent.withdrawn");
        exception.Message.ShouldNotContain("patient-7f3a");
    }

    [Fact]
    public async Task PublishAsync_CancelledLeft_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var adapter = CreateNotificationAdapter(EncinaErrors.Create(EncinaErrorCodes.NotificationCancelled, "cancelled"));

        var exception = await Should.ThrowAsync<OperationCanceledException>(() => adapter.PublishAsync(new SpikeNotification("payload"), cts.Token));

        exception.CancellationToken.ShouldBe(cts.Token);
    }

    // ─── Helpers ───

    private static HangfireRequestJobAdapter<SpikeRequest, SpikeResponse> CreateRequestAdapter(Either<EncinaError, SpikeResponse> outcome)
    {
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>()).Returns(outcome);
        return new HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>(
            encina, NullLogger<HangfireRequestJobAdapter<SpikeRequest, SpikeResponse>>.Instance);
    }

    private static HangfireNotificationJobAdapter<SpikeNotification> CreateNotificationAdapter(EncinaError error)
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<SpikeNotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)));
        return new HangfireNotificationJobAdapter<SpikeNotification>(
            encina, NullLogger<HangfireNotificationJobAdapter<SpikeNotification>>.Instance);
    }

    private sealed class PoisonMessageException : Exception
    {
    }

    /// <summary>A custom classifier that decides by exception type only.</summary>
    private sealed class PoisonMessageClassifier : IErrorClassifier
    {
        public ErrorClassification Classify(EncinaError encinaError, Exception? exception) =>
            exception is PoisonMessageException ? ErrorClassification.Permanent : ErrorClassification.Transient;
    }

    public sealed record SpikeRequest(string Payload) : IRequest<SpikeResponse>;

    public sealed record SpikeResponse(string Result);

    public sealed record SpikeNotification(string Payload) : INotification;
}
