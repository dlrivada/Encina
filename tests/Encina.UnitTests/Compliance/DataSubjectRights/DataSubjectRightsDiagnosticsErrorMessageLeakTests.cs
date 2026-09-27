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
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// <see cref="EncinaError.Message"/> must never reach an OpenTelemetry activity tag, activity
/// status description or metric tag produced by <c>Encina.Compliance.DataSubjectRights</c>
/// diagnostics (#1454, following #1168, #1173, #1259, #1274). Only the error code
/// (<c>error.GetCode().IfNone("encina.unknown")</c>) may be tagged. Covers
/// <see cref="DataSubjectRightsDiagnostics.RecordFailed(Activity?, string)"/> call sites in
/// <see cref="DefaultDSRService"/>, <see cref="DefaultDataErasureExecutor"/>,
/// <see cref="DefaultDataPortabilityExporter"/> and
/// <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DataSubjectRightsDiagnosticsErrorMessageLeakTests
{
    private const string SubjectId = "patient-42";

    // A sentinel that must never appear in any activity tag or status description. Chosen to be
    // extremely unlikely to collide with any error code or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-ERROR-MESSAGE-do-not-tag-this-9f3c1e";

    #region DefaultDSRService

    [Fact]
    public async Task HandleAccessAsync_LocatorFails_NeverTagsErrorMessage()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                EncinaErrors.Create("dsr.locate.failed", SentinelMessage))));

        var (service, _) = CreateService(locator: locator);
        using var capture = CreateCapture();

        var request = new AccessRequest(SubjectId, IncludeProcessingActivities: false);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleAccessAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    [Fact]
    public async Task HandleErasureAsync_ExecutorFails_NeverTagsErrorMessage()
    {
        var erasureExecutor = Substitute.For<IDataErasureExecutor>();
        erasureExecutor.EraseAsync(SubjectId, Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ErasureResult>(
                EncinaErrors.Create("dsr.erasure.failed", SentinelMessage)));

        var (service, _) = CreateService(erasureExecutor: erasureExecutor);
        using var capture = CreateCapture();

        var request = new ErasureRequest(SubjectId, ErasureReason.NoLongerNecessary, Scope: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleErasureAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    [Fact]
    public async Task HandleRestrictionAsync_RepositoryFails_NeverTagsErrorMessage()
    {
        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaErrors.Create("dsr.restriction.failed", SentinelMessage)));

        var (service, _) = CreateService(repository: repository);
        using var capture = CreateCapture();

        var request = new RestrictionRequest(SubjectId, "Accuracy contested", Scope: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleRestrictionAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    [Fact]
    public async Task HandlePortabilityAsync_ExporterFails_NeverTagsErrorMessage()
    {
        var portabilityExporter = Substitute.For<IDataPortabilityExporter>();
        portabilityExporter.ExportAsync(SubjectId, ExportFormat.JSON, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, PortabilityResponse>(
                EncinaErrors.Create("dsr.portability.failed", SentinelMessage)));

        var (service, _) = CreateService(portabilityExporter: portabilityExporter);
        using var capture = CreateCapture();

        var request = new PortabilityRequest(SubjectId, ExportFormat.JSON, Categories: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandlePortabilityAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    private static (DefaultDSRService Service, object Logger) CreateService(
        IAggregateRepository<DSRRequestAggregate>? repository = null,
        IPersonalDataLocator? locator = null,
        IDataErasureExecutor? erasureExecutor = null,
        IDataPortabilityExporter? portabilityExporter = null)
    {
        var service = new DefaultDSRService(
            repository ?? Substitute.For<IAggregateRepository<DSRRequestAggregate>>(),
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            locator ?? Substitute.For<IPersonalDataLocator>(),
            erasureExecutor ?? Substitute.For<IDataErasureExecutor>(),
            portabilityExporter ?? Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(),
            NullLogger<DefaultDSRService>.Instance);
        return (service, NullLogger<DefaultDSRService>.Instance);
    }

    #endregion

    #region DefaultDataErasureExecutor

    [Fact]
    public async Task EraseAsync_LocatorFails_NeverTagsErrorMessage()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                EncinaErrors.Create("dsr.locate.failed", SentinelMessage))));
        var strategy = Substitute.For<IDataErasureStrategy>();

        var sut = new DefaultDataErasureExecutor(locator, strategy, NullLogger<DefaultDataErasureExecutor>.Instance);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => sut.EraseAsync(SubjectId, new ErasureScope { Reason = ErasureReason.NoLongerNecessary }).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    #endregion

    #region DefaultDataPortabilityExporter

    [Fact]
    public async Task ExportAsync_WriterFails_NeverTagsErrorMessage()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = SubjectId,
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = true,
            HasLegalRetention = false
        };

        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                (IReadOnlyList<PersonalDataLocation>)new List<PersonalDataLocation> { location })));

        var writer = Substitute.For<IExportFormatWriter>();
        writer.SupportedFormat.Returns(ExportFormat.JSON);
        writer.WriteAsync(Arg.Any<IReadOnlyList<PersonalDataLocation>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, ExportedData>(
                EncinaErrors.Create("dsr.export.write_failed", SentinelMessage))));

        var sut = new DefaultDataPortabilityExporter(
            locator, [writer], new FakeTimeProvider(), NullLogger<DefaultDataPortabilityExporter>.Instance);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => sut.ExportAsync(SubjectId, ExportFormat.JSON).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    #endregion

    #region ProcessingRestrictionPipelineBehavior

    [RestrictProcessing(SubjectIdProperty = nameof(SubjectIdValue))]
    private sealed record RestrictedCommand(string SubjectIdValue) : IRequest<Unit>;

    [Fact]
    public async Task Handle_StoreError_NeverTagsErrorMessage()
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaErrors.Create("dsr.restriction.store_error", SentinelMessage)));

        var options = Options.Create(new DataSubjectRightsOptions { RestrictionEnforcementMode = DSREnforcementMode.Block });
        var behavior = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, Substitute.For<IDataSubjectIdExtractor>(), options,
            NullLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>.Instance);
        using var capture = CreateCapture();

        var command = new RestrictedCommand(SubjectId);
        var result = await DiagnosticsCapture.CaptureAsync(() => behavior.Handle(
            command, Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, Unit>(unit)), CancellationToken.None).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSentinel();
    }

    #endregion

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
#pragma warning restore CA2012
