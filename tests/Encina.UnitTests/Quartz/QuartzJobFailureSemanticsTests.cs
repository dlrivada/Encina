using Encina.Messaging.Recoverability;
using Encina.Quartz;
using LanguageExt;
using Microsoft.Extensions.Logging.Abstractions;
using Quartz;
using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly - Required for NSubstitute mocking pattern

namespace Encina.UnitTests.Quartz;

/// <summary>
/// Failure semantics of the Quartz jobs (#1159 review). <see cref="QuartzNotificationJob{TNotification}"/>
/// used to discard the <c>Either</c> returned by <c>IEncina.Publish</c>, so a failed notification was
/// recorded as a successful run. Both jobs now throw <see cref="OperationCanceledException"/> for a
/// cancelled job and a <see cref="JobExecutionException"/> (never refired, error code and classification
/// in <see cref="Exception.Data"/>, no <see cref="EncinaError.Message"/>) for any other failure.
/// </summary>
public sealed class QuartzJobFailureSemanticsTests
{
    private const string SensitiveMessage = "Processing is restricted for data subject 'patient-7f3a'";

    // ─── Notification job ───

    [Fact]
    public async Task NotificationJob_Left_ThrowsJobExecutionException()
    {
        var job = CreateNotificationJob(EncinaErrors.Create("test.error", "Handler rejected the notification"));
        var context = CreateContext(QuartzConstants.NotificationKey, new SpikeNotification("payload"));

        var exception = await Should.ThrowAsync<JobExecutionException>(() => job.Execute(context));

        exception.RefireImmediately.ShouldBeFalse();
        exception.Data[EncinaJobFailureData.ErrorCodeKey].ShouldBe("test.error");
        exception.Data[EncinaJobFailureData.ErrorClassificationKey].ShouldBe("Transient");
    }

    [Fact]
    public async Task NotificationJob_PermanentLeft_RecordsPermanentClassification_WithoutTheMessage()
    {
        var job = CreateNotificationJob(EncinaErrors.Create("dsr.restriction_active", SensitiveMessage));
        var context = CreateContext(QuartzConstants.NotificationKey, new SpikeNotification("payload"));

        var exception = await Should.ThrowAsync<JobExecutionException>(() => job.Execute(context));

        exception.Data[EncinaJobFailureData.ErrorClassificationKey].ShouldBe("Permanent");
        exception.Message.ShouldContain("dsr.restriction_active");
        exception.Message.ShouldNotContain("patient-7f3a");
    }

    [Fact]
    public async Task NotificationJob_CancelledLeft_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var job = CreateNotificationJob(EncinaErrors.Create(EncinaErrorCodes.NotificationCancelled, "cancelled"));
        var context = CreateContext(QuartzConstants.NotificationKey, new SpikeNotification("payload"), cts.Token);

        var exception = await Should.ThrowAsync<OperationCanceledException>(() => job.Execute(context));

        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task NotificationJob_Right_Completes()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<SpikeNotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(Unit.Default)));
        var job = new QuartzNotificationJob<SpikeNotification>(encina, NullLogger<QuartzNotificationJob<SpikeNotification>>.Instance);

        await Should.NotThrowAsync(() => job.Execute(CreateContext(QuartzConstants.NotificationKey, new SpikeNotification("payload"))));
    }

    // ─── Request job ───

    [Fact]
    public async Task RequestJob_CancelledLeft_WithCancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var job = CreateRequestJob(EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, "cancelled"));
        var context = CreateContext(QuartzConstants.RequestKey, new SpikeRequest("payload"), cts.Token);

        var exception = await Should.ThrowAsync<OperationCanceledException>(() => job.Execute(context));

        exception.CancellationToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task RequestJob_CancelledLeft_WithoutCancelledToken_IsAJobExecutionException()
    {
        var job = CreateRequestJob(EncinaErrors.Create(EncinaErrorCodes.RequestCancelled, "cancelled by the handler"));
        var context = CreateContext(QuartzConstants.RequestKey, new SpikeRequest("payload"));

        await Should.ThrowAsync<JobExecutionException>(() => job.Execute(context));
    }

    [Fact]
    public async Task RequestJob_LeftWithException_PassesItAsInnerException()
    {
        var cause = new TimeoutException("db timeout");
        var job = CreateRequestJob(EncinaErrors.Create("store.failure", "Store failed", cause));
        var context = CreateContext(QuartzConstants.RequestKey, new SpikeRequest("payload"));

        var exception = await Should.ThrowAsync<JobExecutionException>(() => job.Execute(context));

        exception.InnerException.ShouldBeSameAs(cause);
        exception.Data[EncinaJobFailureData.ErrorClassificationKey].ShouldBe("Transient");
    }

    [Fact]
    public async Task RequestJob_UsesTheInjectedErrorClassifier()
    {
        var classifier = Substitute.For<IErrorClassifier>();
        classifier.Classify(Arg.Any<EncinaError>(), Arg.Any<Exception?>()).Returns(ErrorClassification.Permanent);
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(EncinaErrors.Create("custom.domain_rule", "rule broken")));
        var job = new QuartzRequestJob<SpikeRequest, SpikeResponse>(
            encina, NullLogger<QuartzRequestJob<SpikeRequest, SpikeResponse>>.Instance, classifier);

        var exception = await Should.ThrowAsync<JobExecutionException>(() =>
            job.Execute(CreateContext(QuartzConstants.RequestKey, new SpikeRequest("payload"))));

        exception.Data[EncinaJobFailureData.ErrorClassificationKey].ShouldBe("Permanent");
    }

    // ─── Helpers ───

    private static QuartzNotificationJob<SpikeNotification> CreateNotificationJob(EncinaError error)
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<SpikeNotification>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Either<EncinaError, Unit>>(Left<EncinaError, Unit>(error)));
        return new QuartzNotificationJob<SpikeNotification>(encina, NullLogger<QuartzNotificationJob<SpikeNotification>>.Instance);
    }

    private static QuartzRequestJob<SpikeRequest, SpikeResponse> CreateRequestJob(EncinaError error)
    {
        var encina = Substitute.For<IEncina>();
        encina.Send(Arg.Any<SpikeRequest>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, SpikeResponse>(error));
        return new QuartzRequestJob<SpikeRequest, SpikeResponse>(encina, NullLogger<QuartzRequestJob<SpikeRequest, SpikeResponse>>.Instance);
    }

    private static IJobExecutionContext CreateContext(string key, object payload, CancellationToken cancellationToken = default)
    {
        var jobDetail = Substitute.For<IJobDetail>();
        jobDetail.JobDataMap.Returns(new JobDataMap { { key, payload } });
        jobDetail.Key.Returns(new JobKey("failure-semantics-job"));

        var context = Substitute.For<IJobExecutionContext>();
        context.JobDetail.Returns(jobDetail);
        context.CancellationToken.Returns(cancellationToken);
        return context;
    }

    public sealed record SpikeRequest(string Payload) : IRequest<SpikeResponse>;

    public sealed record SpikeResponse(string Result);

    public sealed record SpikeNotification(string Payload) : INotification;
}
