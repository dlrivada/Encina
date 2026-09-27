using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Diagnostics;
using Encina.Compliance.Retention.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// <see cref="EncinaError.Message"/> must never reach an OpenTelemetry activity tag, activity
/// status description or metric tag produced by <c>Encina.Compliance.Retention</c> diagnostics
/// (#1454, following #1168, #1173, #1259, #1274). Only the error code
/// (<c>error.GetCode().IfNone("encina.unknown")</c>) may be tagged. Covers the
/// <see cref="RetentionEnforcementService"/> failure path into
/// <see cref="RetentionDiagnostics.RecordFailed(Activity?, string)"/>.
/// </summary>
public sealed class RetentionDiagnosticsErrorMessageLeakTests
{
    // A sentinel that must never appear in any activity tag or status description.
    private const string SentinelMessage = "SENTINEL-ERROR-MESSAGE-do-not-tag-this-9f3c1e";

    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_GetExpiredRecordsFails_NeverTagsErrorMessage()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                EncinaErrors.Create("retention.enforcement.store_error", SentinelMessage)));

        var legalHoldService = Substitute.For<ILegalHoldService>();

        var services = new ServiceCollection();
        services.AddSingleton(recordService);
        services.AddSingleton(legalHoldService);
        var provider = services.BuildServiceProvider();

        var sut = new RetentionEnforcementService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RetentionOptions
            {
                EnableAutomaticEnforcement = true,
                EnforcementInterval = TimeSpan.FromHours(1),
                PublishNotifications = false
            }),
            NullLogger<RetentionEnforcementService>.Instance,
            new FakeTimeProvider(Now));

        using var capture = CreateCapture();

        await DiagnosticsCapture.CaptureAsync(() => sut.ExecuteEnforcementCycleAsync(CancellationToken.None));

        capture.AssertNoSentinel();
    }

    private static DiagnosticsCapture CreateCapture() =>
        new(RetentionDiagnostics.ActivitySource, RetentionDiagnostics.Meter);

    /// <summary>
    /// Captures every activity tag/status description and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only for the calls made through
    /// <see cref="DiagnosticsCapture.CaptureAsync"/> — an <see cref="AsyncLocal{T}"/> flag scopes
    /// capture to this test's own logical call, so a process-global listener never picks up another
    /// test's concurrently running activity on the same shared source (#1454; see #1423).
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private static readonly AsyncLocal<bool> Capturing = new();

        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public DiagnosticsCapture(ActivitySource activitySource, Meter meter)
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == activitySource.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    if (!Capturing.Value)
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

        private void CaptureTags(ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            if (!Capturing.Value)
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

        public static async Task CaptureAsync(Func<Task> action)
        {
            Capturing.Value = true;
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                Capturing.Value = false;
            }
        }

        public void AssertNoSentinel()
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
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(SentinelMessage);
            }

            foreach (var tagValue in tagValuesSnapshot)
            {
                if (tagValue is string tagString)
                {
                    tagString.ShouldNotContain(SentinelMessage);
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
