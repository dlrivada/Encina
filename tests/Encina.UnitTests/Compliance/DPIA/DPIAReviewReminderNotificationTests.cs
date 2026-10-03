#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;
using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Tests the notification publishing of <see cref="DPIAReviewReminderService"/>: the expired
/// assessments found in a cycle are published through <see cref="IEncina"/>, and a failure
/// there never fails the cycle nor leaks the exception message to the logs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "DPIA")]
public sealed class DPIAReviewReminderNotificationTests
{
    private const string SentinelMessage = "SENTINEL-publish-failure-message";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IDPIAService _service = Substitute.For<IDPIAService>();
    private readonly IEncina _encina = Substitute.For<IEncina>();

    private static DPIAReadModel Expired(string requestTypeName) => new()
    {
        Id = Guid.NewGuid(),
        RequestTypeName = requestTypeName,
        Status = DPIAAssessmentStatus.Approved,
        NextReviewAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
    };

    private void SetupExpired(params DPIAReadModel[] expired)
        => _service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<DPIAReadModel>>(expired)));

    private DPIAReviewReminderService CreateSut(bool registerEncina, ILogger<DPIAReviewReminderService> logger)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_service);
        services.AddSingleton<TimeProvider>(_time);

        if (registerEncina)
        {
            services.AddSingleton(_encina);
        }

        var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

        return new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(new DPIAOptions
            {
                EnableExpirationMonitoring = true,
                ExpirationCheckInterval = TimeSpan.FromHours(1),
                PublishNotifications = true
            }),
            logger);
    }

    [Fact]
    public async Task Cycle_ExpiredAssessments_PublishesOneNotificationPerAssessment()
    {
        var first = Expired("Orders.CreditScoring");
        var second = Expired("Orders.HealthProfiling");
        SetupExpired(first, second);

        var published = new List<DPIAAssessmentExpired>();
        var bothPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _encina.Publish(Arg.Any<DPIAAssessmentExpired>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                lock (published)
                {
                    published.Add(call.Arg<DPIAAssessmentExpired>());
                    if (published.Count == 2)
                    {
                        bothPublished.TrySetResult();
                    }
                }

                return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
            });

        var sut = CreateSut(registerEncina: true, new ComplianceLogCapture<DPIAReviewReminderService>());

        await sut.StartAsync(CancellationToken.None);
        await bothPublished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);

        published.Count.ShouldBe(2);
        published[0].AssessmentId.ShouldBe(first.Id);
        published[0].RequestTypeName.ShouldBe("Orders.CreditScoring");
        published[0].ExpiredAtUtc.ShouldBe(_time.GetUtcNow());
        published[1].AssessmentId.ShouldBe(second.Id);
    }

    [Fact]
    public async Task Cycle_PublishThrows_LogsRedactedExceptionAndDoesNotFailTheService()
    {
        SetupExpired(Expired("Orders.CreditScoring"));
        _encina.Publish(Arg.Any<DPIAAssessmentExpired>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(SentinelMessage));

        var logger = new ComplianceLogCapture<DPIAReviewReminderService>(e => e.Exception is not null);
        var sut = CreateSut(registerEncina: true, logger);

        await sut.StartAsync(CancellationToken.None);
        await logger.Signaled.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);

        var failure = logger.Entries.Single(e => e.Exception is not null);
        failure.Level.ShouldBe(LogLevel.Error);
        failure.Exception.ShouldBeOfType<RedactedException>();
        failure.Exception!.Message.ShouldNotContain(SentinelMessage);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task Cycle_NoEncinaRegistered_SkipsPublishingWithoutFailing()
    {
        SetupExpired(Expired("Orders.CreditScoring"));

        var logger = new ComplianceLogCapture<DPIAReviewReminderService>(e => e.Message.Contains("require review"));
        var sut = CreateSut(registerEncina: false, logger);

        await sut.StartAsync(CancellationToken.None);
        await logger.Signaled.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);

        logger.Entries.ShouldAllBe(e => e.Exception == null);
    }
}
