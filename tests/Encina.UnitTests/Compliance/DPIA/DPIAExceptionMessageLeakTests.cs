using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Abstractions;
using Encina.Compliance.DPIA.Diagnostics;
using Encina.Compliance.DPIA.Model;
using Encina.Compliance.DPIA.ReadModels;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// A caught <see cref="System.Exception"/>'s own <c>Message</c> must never reach an OpenTelemetry
/// activity tag or activity status description produced by <c>Encina.Compliance.DPIA</c>
/// diagnostics (#1488, extending #1454's <see cref="EncinaError.Message"/> rule to exception
/// messages, per AGENTS.md: "record only the error code or the exception type"). Only
/// <c>ex.GetType().Name</c> may be tagged. Covers the <see cref="DPIAReviewReminderService"/>
/// catch-all path into <see cref="DPIADiagnostics.RecordFailed(Activity?, string)"/> and the
/// <see cref="DefaultDPIAAssessmentEngine.AssessAsync"/> catch block into
/// <see cref="DPIADiagnostics.RecordAssessmentFailed(Activity?, string)"/>.
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DPIAExceptionMessageLeakTests
{
    // A sentinel that must never appear in any activity tag or status description. Chosen to be
    // extremely unlikely to collide with any error code or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-EXCEPTION-MESSAGE-do-not-tag-this-7ad91b";

    #region DPIAReviewReminderService

    [Fact]
    public async Task ReviewReminderCycle_StoreThrows_NeverTagsExceptionMessage()
    {
        var options = new DPIAOptions
        {
            EnableExpirationMonitoring = true,
            ExpirationCheckInterval = TimeSpan.FromHours(1),
            PublishNotifications = false
        };

        var service = Substitute.For<IDPIAService>();
        service.GetExpiredAssessmentsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<DPIAReadModel>>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        var scopeFactory = CreateScopeFactory(service);
        var sut = new DPIAReviewReminderService(
            scopeFactory,
            Options.Create(options),
            NullLogger<DPIAReviewReminderService>.Instance);

        using var capture = new BackgroundCapture(DPIADiagnostics.ActivitySource, DPIADiagnostics.Meter);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        await capture.RunAsync(async () =>
        {
            await sut.StartAsync(cts.Token);
            await WaitUntilAsync(() => capture.HasActivities, TimeSpan.FromSeconds(5));
            await sut.StopAsync(CancellationToken.None);
        });

        capture.AssertNoSentinel(SentinelMessage);
    }

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

    /// <summary>
    /// Captures every activity tag/status description and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only while
    /// <see cref="RunAsync"/> is in flight — this scopes capture to this test's own
    /// background-service run, so a process-global listener never picks up another test's
    /// concurrently running activity on the same shared source (#1454; see #1423).
    /// </summary>
    private sealed class BackgroundCapture : IDisposable
    {
        private readonly AsyncLocal<bool> _capturing = new();
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public BackgroundCapture(ActivitySource activitySource, Meter meter)
        {
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

        public async Task RunAsync(Func<Task> action)
        {
            _capturing.Value = true;
            try
            {
                await action().ConfigureAwait(false);
            }
            finally
            {
                _capturing.Value = false;
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

    #endregion

    #region DefaultDPIAAssessmentEngine

    [Fact]
    public async Task AssessAsync_TemplateProviderThrows_NeverTagsExceptionMessage()
    {
        var templateProvider = Substitute.For<IDPIATemplateProvider>();
        templateProvider.GetTemplateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, DPIATemplate>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        var engine = new DefaultDPIAAssessmentEngine(
            [],
            templateProvider,
            Options.Create(new DPIAOptions()),
            new FakeTimeProvider(),
            NullLogger<DefaultDPIAAssessmentEngine>.Instance);

        var context = new DPIAContext
        {
            RequestType = typeof(object),
            ProcessingType = "AutomatedDecisionMaking",
            DataCategories = [],
            HighRiskTriggers = [],
        };

        using var capture = new CallCapture(DPIADiagnostics.ActivitySource, DPIADiagnostics.Meter);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(
            () => capture.RunAsync(() => engine.AssessAsync(context).AsTask()));

        thrown.Message.ShouldBe(SentinelMessage);
        capture.AssertNoSentinel(SentinelMessage);
    }

    /// <summary>
    /// Captures every activity tag/status description and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only for the calls made through
    /// <see cref="RunAsync{T}"/> — an <see cref="AsyncLocal{T}"/> flag scopes capture to this
    /// test's own logical call, so a process-global listener never picks up another test's
    /// concurrently running activity on the same shared source (#1454; see #1423).
    /// </summary>
    private sealed class CallCapture : IDisposable
    {
        private readonly AsyncLocal<bool> _capturing = new();
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly Lock _sync = new();
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public CallCapture(ActivitySource activitySource, Meter meter)
        {
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

        public async Task<T> RunAsync<T>(Func<Task<T>> action)
        {
            _capturing.Value = true;
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _capturing.Value = false;
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

    #endregion
}
#pragma warning restore CA2012
