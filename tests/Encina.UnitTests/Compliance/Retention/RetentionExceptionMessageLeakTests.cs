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
/// A caught <see cref="System.Exception"/>'s own <c>Message</c> must never reach an OpenTelemetry
/// activity tag or activity status description produced by <c>Encina.Compliance.Retention</c>
/// diagnostics (#1488, extending #1454's <see cref="EncinaError.Message"/> rule to exception
/// messages, per AGENTS.md: "record only the error code or the exception type"). Only
/// <c>ex.GetType().Name</c> may be tagged. Covers the
/// <see cref="RetentionValidationPipelineBehavior{TRequest, TResponse}"/> and
/// <see cref="RetentionEnforcementService"/> catch blocks that feed
/// <see cref="RetentionDiagnostics.RecordFailed(Activity?, string)"/>.
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class RetentionExceptionMessageLeakTests
{
    // A sentinel that must never appear in any activity tag or status description. Chosen to be
    // extremely unlikely to collide with any error code or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-EXCEPTION-MESSAGE-do-not-tag-this-7ad91b";

    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    #region RetentionValidationPipelineBehavior

    [RetentionPeriod(Days = 365, DataCategory = "test-category", Reason = "Test")]
    private sealed record DecoratedResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed record DecoratedCommand : IRequest<DecoratedResponse>;

    [Fact]
    public async Task Handle_TrackEntityThrows_NeverTagsExceptionMessage()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.TrackEntityAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Guid>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        var behavior = new RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            recordService,
            Substitute.For<IRetentionPolicyService>(),
            Options.Create(new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Block }),
            NullLogger<RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>>.Instance);

        using var capture = new CallCapture(RetentionDiagnostics.ActivitySource, RetentionDiagnostics.Meter);

        var response = new DecoratedResponse { Id = "entity-42" };
        var result = await capture.RunAsync(() => behavior.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    #endregion

    #region RetentionEnforcementService

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_GetExpiredRecordsThrows_NeverTagsExceptionMessage()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

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

        using var capture = new CallCapture(RetentionDiagnostics.ActivitySource, RetentionDiagnostics.Meter);

        await capture.RunAsync(async () =>
        {
            await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
            return unit;
        });

        capture.AssertNoSentinel();
    }

    #endregion

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
#pragma warning restore CA2012
