#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method

using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Abstractions;
using Encina.Compliance.BreachNotification.Model;
using Encina.Compliance.BreachNotification.ReadModels;
using Encina.Diagnostics;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.BreachNotification;

/// <summary>
/// Tests the monitoring cycle and the deadline warning publishing of
/// <see cref="BreachDeadlineMonitorService"/>. Cycles are awaited through log signals, never sleeps.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "BreachNotification")]
public sealed class BreachDeadlineMonitorServiceTests
{
    private const string SentinelMessage = "SENTINEL-breach-monitor-message";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly IBreachNotificationService _breachService = Substitute.For<IBreachNotificationService>();
    private readonly IEncina _encina = Substitute.For<IEncina>();

    private BreachReadModel Breach(double hoursRemaining) => new()
    {
        Id = Guid.NewGuid(),
        DeadlineUtc = _time.GetUtcNow().AddHours(hoursRemaining)
    };

    private void SetupApproaching(params BreachReadModel[] breaches)
        => _breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<BreachReadModel>>(breaches)));

    private BreachDeadlineMonitorService CreateSut(
        bool registerEncina,
        ILogger<BreachDeadlineMonitorService> logger,
        bool publishNotifications = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_breachService);

        if (registerEncina)
        {
            services.AddSingleton(_encina);
        }

        return new BreachDeadlineMonitorService(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new BreachNotificationOptions
            {
                DeadlineCheckInterval = TimeSpan.FromHours(1),
                PublishNotifications = publishNotifications
            }),
            _time,
            logger);
    }

    private static async Task RunFirstCycleAsync(BreachDeadlineMonitorService sut, ComplianceLogCapture<BreachDeadlineMonitorService> logger)
    {
        await sut.StartAsync(CancellationToken.None);
        await logger.Signaled.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Cycle_ApproachingBreaches_PublishesOneWarningPerBreach()
    {
        var urgent = Breach(5);
        var later = Breach(20);
        SetupApproaching(urgent, later);

        var published = new List<DeadlineWarningNotification>();
        var bothPublished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _encina.Publish(Arg.Any<DeadlineWarningNotification>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                lock (published)
                {
                    published.Add(call.Arg<DeadlineWarningNotification>());
                    if (published.Count == 2)
                    {
                        bothPublished.TrySetResult();
                    }
                }

                return ValueTask.FromResult(Right<EncinaError, Unit>(Unit.Default));
            });

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>();
        var sut = CreateSut(registerEncina: true, logger);

        await sut.StartAsync(CancellationToken.None);
        await bothPublished.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);

        published.Count.ShouldBe(2);
        published[0].BreachId.ShouldBe(urgent.Id.ToString());
        published[0].RemainingHours.ShouldBe(5, 0.001);
        published[0].DeadlineUtc.ShouldBe(urgent.DeadlineUtc);
        published[1].BreachId.ShouldBe(later.Id.ToString());
        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("approaching notification deadline"));
    }

    [Fact]
    public async Task Cycle_PublishThrows_LogsRedactedExceptionAndKeepsRunning()
    {
        SetupApproaching(Breach(5));
        _encina.Publish(Arg.Any<DeadlineWarningNotification>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(SentinelMessage));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Exception is not null);
        var sut = CreateSut(registerEncina: true, logger);

        await RunFirstCycleAsync(sut, logger);

        var failure = logger.Entries.Single(e => e.Exception is not null);
        failure.Level.ShouldBe(LogLevel.Warning);
        failure.Exception.ShouldBeOfType<RedactedException>();
        failure.Exception!.Message.ShouldNotContain(SentinelMessage);
        logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task Cycle_NoEncinaRegistered_SkipsPublishing()
    {
        SetupApproaching(Breach(5));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Message.Contains("approaching notification deadline"));
        var sut = CreateSut(registerEncina: false, logger);

        await RunFirstCycleAsync(sut, logger);

        logger.Entries.ShouldAllBe(e => e.Exception == null);
    }

    [Fact]
    public async Task Cycle_PublishingDisabled_DoesNotPublish()
    {
        SetupApproaching(Breach(5));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Message.Contains("approaching notification deadline"));
        var sut = CreateSut(registerEncina: true, logger, publishNotifications: false);

        await RunFirstCycleAsync(sut, logger);

        await _encina.DidNotReceiveWithAnyArgs().Publish(default(DeadlineWarningNotification)!, default);
    }

    [Fact]
    public async Task Cycle_OverdueBreach_LogsErrorWithHoursOverdue()
    {
        SetupApproaching(Breach(-3));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Level == LogLevel.Error);
        var sut = CreateSut(registerEncina: false, logger);

        await RunFirstCycleAsync(sut, logger);

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Error && e.Message.Contains("exceeded the 72-hour"));
    }

    [Fact]
    public async Task Cycle_NoApproachingBreaches_LogsNothingAboutBreaches()
    {
        var queried = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                queried.TrySetResult();
                return ValueTask.FromResult(
                    Right<EncinaError, IReadOnlyList<BreachReadModel>>(System.Array.Empty<BreachReadModel>()));
            });

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>();
        var sut = CreateSut(registerEncina: true, logger);

        await sut.StartAsync(CancellationToken.None);
        await queried.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await sut.StopAsync(CancellationToken.None);

        logger.Entries.ShouldNotContain(e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task Cycle_QueryReturnsError_LogsErrorCodeOnly()
    {
        _breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<BreachReadModel>>(EncinaErrors.Create("breach.store_error", SentinelMessage))));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Message.Contains("breach.store_error"));
        var sut = CreateSut(registerEncina: true, logger);

        await RunFirstCycleAsync(sut, logger);

        logger.Entries.ShouldAllBe(e => !e.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task Cycle_QueryThrows_LogsRedactedExceptionAndDoesNotCrash()
    {
        _breachService.GetApproachingDeadlineBreachesAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<BreachReadModel>>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        var logger = new ComplianceLogCapture<BreachDeadlineMonitorService>(e => e.Exception is not null);
        var sut = CreateSut(registerEncina: true, logger);

        await RunFirstCycleAsync(sut, logger);

        var failure = logger.Entries.Single(e => e.Exception is not null);
        failure.Level.ShouldBe(LogLevel.Error);
        failure.Exception.ShouldBeOfType<RedactedException>();
        failure.Exception!.Message.ShouldNotContain(SentinelMessage);
    }
}
