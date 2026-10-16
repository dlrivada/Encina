using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// The deny path counts refusals by reason, traces them and logs them without the data subject,
/// the explanation text or the tenant in metric tags or logs (#814, #1429).
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DataSubjectRightsDenyTelemetryTests
{
    private const string SubjectId = "patient-77";
    private const string Explanation = "SENTINEL-EXPLANATION-do-not-log-3c1f";
    private const string TenantId = "tenant-9";

    private static readonly AsyncLocal<bool> Capturing = new();

    [Fact]
    public async Task DenyRequestAsync_Success_CountsDenialWithReasonTagAndTracesTenant()
    {
        var (service, logger) = CreateService(TenantId);
        var measurements = new List<Dictionary<string, object?>>();
        var activities = new List<Activity>();
        using var meterListener = StartMeterListener(measurements);
        using var activityListener = StartActivityListener(activities);

        var result = await CaptureAsync(() => service.DenyRequestAsync(
            Guid.NewGuid(), DSRRejectionReason.Excessive, Explanation).AsTask());

        result.IsRight.ShouldBeTrue();

        var measurement = measurements.ShouldHaveSingleItem();
        measurement.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray()
            .ShouldBe([DataSubjectRightsDiagnostics.TagRejectionReason, DataSubjectRightsDiagnostics.TagRightType]);
        measurement[DataSubjectRightsDiagnostics.TagRejectionReason].ShouldBe("Excessive");
        measurement[DataSubjectRightsDiagnostics.TagRightType].ShouldBe("Access");

        var activity = activities.ShouldHaveSingleItem();
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagOperation).ShouldBe("Deny");
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagRejectionReason).ShouldBe("Excessive");
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagTenantId).ShouldBe(TenantId);
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagOutcome).ShouldBe("completed");

        var denied = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8311);
        denied.Level.ShouldBe(LogLevel.Information);
        AssertNoPersonalData(logger, activities, measurements);
    }

    [Fact]
    public async Task DenyRequestAsync_DraftGroundNotEnabled_LogsWarningAndDoesNotCount()
    {
        var (service, logger) = CreateService(tenantId: null);
        var measurements = new List<Dictionary<string, object?>>();
        var activities = new List<Activity>();
        using var meterListener = StartMeterListener(measurements);
        using var activityListener = StartActivityListener(activities);

        var result = await CaptureAsync(() => service.DenyRequestAsync(
            Guid.NewGuid(), DSRRejectionReason.AbusiveIntent, Explanation).AsTask());

        result.IsLeft.ShouldBeTrue();
        measurements.ShouldBeEmpty();

        var warning = logger.Collector.GetSnapshot().Single(r => r.Id.Id == 8312);
        warning.Level.ShouldBe(LogLevel.Warning);

        var activity = activities.ShouldHaveSingleItem();
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagOutcome).ShouldBe("failed");
        activity.GetTagItem(DataSubjectRightsDiagnostics.TagFailureReason).ShouldBe(DSRErrors.RejectionReasonNotEnabledCode);
        AssertNoPersonalData(logger, activities, measurements);
    }

    private static void AssertNoPersonalData(
        FakeLogger<DefaultDSRService> logger,
        List<Activity> activities,
        List<Dictionary<string, object?>> measurements)
    {
        logger.Collector.GetSnapshot().ShouldAllBe(r =>
            !r.Message.Contains(SubjectId, StringComparison.Ordinal)
            && !r.Message.Contains(Explanation, StringComparison.Ordinal));

        var values = activities
            .SelectMany(a => a.Tags.Select(t => t.Value))
            .Concat(measurements.SelectMany(m => m.Values))
            .OfType<string>();
        foreach (var value in values)
        {
            value.ShouldNotContain(SubjectId);
            value.ShouldNotContain(Explanation);
        }
    }

    private static (DefaultDSRService Service, FakeLogger<DefaultDSRService> Logger) CreateService(string? tenantId)
    {
        var requestId = Guid.NewGuid();
        var aggregate = DSRRequestAggregate.Submit(
            requestId, SubjectId, DataSubjectRight.Access, new DateTimeOffset(2026, 3, 15, 10, 0, 0, TimeSpan.Zero),
            tenantId: tenantId);
        aggregate.ClearUncommittedEvents();

        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));
        repository.SaveAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var logger = new FakeLogger<DefaultDSRService>();
        var service = new DefaultDSRService(
            repository,
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            Options.Create(new DataSubjectRightsOptions()),
            new FakeTimeProvider(),
            logger);
        return (service, logger);
    }

    private static MeterListener StartMeterListener(List<Dictionary<string, object?>> measurements)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == DataSubjectRightsDiagnostics.Meter.Name
                    && instrument.Name == "dsr.requests.denied.total")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (!Capturing.Value)
            {
                return;
            }

            var captured = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                captured[tag.Key] = tag.Value;
            }

            lock (measurements)
            {
                measurements.Add(captured);
            }
        });
        listener.Start();
        return listener;
    }

    private static ActivityListener StartActivityListener(List<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DataSubjectRightsDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (!Capturing.Value)
                {
                    return;
                }

                lock (activities)
                {
                    activities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static async Task<T> CaptureAsync<T>(Func<Task<T>> action)
    {
        Capturing.Value = true;
        try
        {
            return await action().ConfigureAwait(false);
        }
        finally
        {
            Capturing.Value = false;
        }
    }
}
#pragma warning restore CA2012
