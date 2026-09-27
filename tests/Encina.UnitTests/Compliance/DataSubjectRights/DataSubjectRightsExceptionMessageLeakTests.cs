using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
using Encina.Compliance.DataSubjectRights.Diagnostics;
using Encina.Compliance.DataSubjectRights.Projections;
using Encina.Compliance.DataSubjectRights.Services;
using Encina.Compliance.GDPR;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// A caught <see cref="System.Exception"/>'s own <c>Message</c> must never reach an OpenTelemetry
/// activity tag or activity status description produced by
/// <c>Encina.Compliance.DataSubjectRights</c> diagnostics (#1488, extending #1454's
/// <see cref="EncinaError.Message"/> rule to exception messages, per AGENTS.md: "record only the
/// error code or the exception type"). Only <c>ex.GetType().Name</c> may be tagged. Covers the
/// <see cref="DefaultDSRService.HandleRestrictionAsync"/> catch block that feeds
/// <see cref="DataSubjectRightsDiagnostics.RecordFailed(Activity?, string)"/> via its private
/// <c>RecordFailure</c> helper.
/// </summary>
public sealed class DataSubjectRightsExceptionMessageLeakTests
{
    private const string SubjectId = "patient-42";

    // A sentinel that must never appear in any activity tag or status description. Chosen to be
    // extremely unlikely to collide with any error code or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-EXCEPTION-MESSAGE-do-not-tag-this-7ad91b";

    [Fact]
    public async Task HandleRestrictionAsync_RepositoryThrows_NeverTagsExceptionMessage()
    {
        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(SentinelMessage));

        var service = new DefaultDSRService(
            repository,
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(),
            NullLogger<DefaultDSRService>.Instance);

        using var capture = CreateCapture();

        var request = new RestrictionRequest(SubjectId, "Accuracy contested", Scope: null);
        var result = await DiagnosticsCapture.CaptureAsync(
            () => service.HandleRestrictionAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    private static DiagnosticsCapture CreateCapture() =>
        new(DataSubjectRightsDiagnostics.ActivitySource, DataSubjectRightsDiagnostics.Meter);

    /// <summary>
    /// Captures every activity tag/status description and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only for the calls made through
    /// <see cref="DiagnosticsCapture.CaptureAsync{T}"/> — an <see cref="AsyncLocal{T}"/> flag scopes
    /// capture to this test's own logical call, so a process-global listener never picks up another
    /// test's concurrently running activity on the same shared source (#1454; see #1423, where a
    /// catch-all listener without this scoping made a Messaging test flaky).
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

        public static async Task<T> CaptureAsync<T>(Func<Task<T>> action)
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
