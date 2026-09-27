using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// <see cref="EncinaError.Message"/> must never reach an OpenTelemetry activity tag, activity
/// status description or metric tag produced by <c>Encina.Compliance.DPIA</c> diagnostics
/// (#1454, following #1168, #1173, #1259, #1274). Only the error code
/// (<c>error.GetCode().IfNone("encina.unknown")</c>) may be tagged. Covers the
/// <see cref="DPIAReviewReminderService"/> failure path into
/// <see cref="DPIADiagnostics.RecordFailed(Activity?, string)"/>.
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DPIADiagnosticsErrorMessageLeakTests
{
    // A sentinel that must never appear in any activity tag or status description.
    private const string SentinelMessage = "SENTINEL-ERROR-MESSAGE-do-not-tag-this-9f3c1e";

    [Fact]
    public async Task ReviewReminderCycle_StoreFails_NeverTagsErrorMessage()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>(
                EncinaErrors.Create("dpia.review_reminder.store_error", SentinelMessage)));

        var scopeFactory = CreateScopeFactory(service);
        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            NullLogger<DPIAReviewReminderService>.Instance);

        using var capture = CreateCapture();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        Capturing.Value = true;
        try
        {
            await sut.StartAsync(cts.Token);
            await WaitUntilAsync(() => capture.HasActivities, TimeSpan.FromSeconds(5));
            await sut.StopAsync(CancellationToken.None);
        }
        finally
        {
            Capturing.Value = false;
        }

        capture.AssertNoSentinel(SentinelMessage);
    }

    private static readonly AsyncLocal<bool> Capturing = new();

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20, CancellationToken.None);
        }
    }

    private static IServiceScopeFactory CreateScopeFactory(IDPIAService service)
    {
        var services = new ServiceCollection();
        services.AddSingleton(service);
        services.AddSingleton(TimeProvider.System);

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private static DiagnosticsCapture CreateCapture() =>
        new(DPIADiagnostics.ActivitySource, DPIADiagnostics.Meter, Capturing);

    /// <summary>
    /// Captures every activity tag/status description and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only while the shared
    /// <see cref="AsyncLocal{T}"/> capturing flag is set — this scopes capture to this test's own
    /// background-service run, so a process-global listener never picks up another test's
    /// concurrently running activity on the same shared source (#1454; see #1423).
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private readonly AsyncLocal<bool> _capturing;
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public DiagnosticsCapture(ActivitySource activitySource, Meter meter, AsyncLocal<bool> capturing)
        {
            _capturing = capturing;
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (!_capturing.Value)
                    {
                        return;
                    }

                    lock (_sync)
                    {
                        _activities.Add(activity);
                        foreach (var tag in activity.Tags)
                        {
                            _tagValues.Add(tag.Value);
                        }
                    }
                }
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == meter.Name)
                    {
                        listener.EnableMeasurementEvents(instrument);
                    }
                }
            };
            _meterListener.SetMeasurementEventCallback<long>((_, _, tags, _) => CaptureTags(tags));
            _meterListener.SetMeasurementEventCallback<double>((_, _, tags, _) => CaptureTags(tags));
            _meterListener.Start();
        }

        public bool HasActivities
        {
            get
            {
                lock (_sync)
                {
                    return _activities.Count > 0;
                }
            }
        }

        private void CaptureTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            if (!_capturing.Value)
            {
                return;
            }

            lock (_sync)
            {
                foreach (var tag in tags)
                {
                    _tagValues.Add(tag.Value);
                }
            }
        }

        public void AssertNoSentinel(string sentinel)
        {
            List<Activity> activitiesSnapshot;
            List<object?> tagValuesSnapshot;
            lock (_sync)
            {
                activitiesSnapshot = [.. _activities];
                tagValuesSnapshot = [.. _tagValues];
            }

            activitiesSnapshot.ShouldNotBeEmpty();

            foreach (var activity in activitiesSnapshot)
            {
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(sentinel);
            }

            foreach (var tagValue in tagValuesSnapshot)
            {
                if (tagValue is string tagString)
                {
                    tagString.ShouldNotContain(sentinel);
                }
            }
        }

        public void Dispose()
        {
            _activityListener.Dispose();
            _meterListener.Dispose();
        }
    }
}
#pragma warning restore CA2012
