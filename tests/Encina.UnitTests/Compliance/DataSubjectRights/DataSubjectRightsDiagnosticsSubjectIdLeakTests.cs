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
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// The data subject's own identifier must never reach a log message, an OpenTelemetry activity
/// tag/status, or a metric tag produced by <c>Encina.Compliance.DataSubjectRights</c> diagnostics
/// (#1429, following #1314). Covers <see cref="DataSubjectRightsDiagnostics"/>,
/// <see cref="DSRLogMessages"/> and their call sites in <see cref="DefaultDSRService"/>,
/// <see cref="DefaultDataErasureExecutor"/>, <see cref="DefaultDataPortabilityExporter"/> and
/// <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
/// <remarks>
/// The audit trail is out of scope: an <c>IAuditStore</c> that legitimately records the subject id
/// is the access-controlled place for it (see the knowledge record for #1429).
/// </remarks>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class DataSubjectRightsDiagnosticsSubjectIdLeakTests
{
    private const string SubjectId = "patient-42";

    #region DefaultDSRService

    [Fact]
    public async Task SubmitRequestAsync_NeverCarriesSubjectId()
    {
        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var (service, logger) = CreateService(repository: repository);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => service.SubmitRequestAsync(SubjectId, DataSubjectRight.Access).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: false);
    }

    [Fact]
    public async Task HandleAccessAsync_NeverCarriesSubjectId()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                (IReadOnlyList<PersonalDataLocation>)new List<PersonalDataLocation>())));

        var (service, logger) = CreateService(locator: locator);
        using var capture = CreateCapture();

        var request = new AccessRequest(SubjectId, IncludeProcessingActivities: false);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleAccessAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandleAccessAsync_LocatorFails_NeverCarriesSubjectId()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                DSRErrors.ServiceError("LocateAllData", new InvalidOperationException("boom")))));

        var (service, logger) = CreateService(locator: locator);
        using var capture = CreateCapture();

        var request = new AccessRequest(SubjectId, IncludeProcessingActivities: false);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleAccessAsync(request).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandleErasureAsync_NeverCarriesSubjectId()
    {
        var erasureExecutor = Substitute.For<IDataErasureExecutor>();
        erasureExecutor.EraseAsync(SubjectId, Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ErasureResult>(new ErasureResult
            {
                FieldsErased = 0,
                FieldsRetained = 0,
                FieldsFailed = 0,
                RetentionReasons = [],
                Exemptions = []
            }));

        var (service, logger) = CreateService(erasureExecutor: erasureExecutor);
        using var capture = CreateCapture();

        var request = new ErasureRequest(SubjectId, ErasureReason.NoLongerNecessary, Scope: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleErasureAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandleRectificationAsync_NeverCarriesSubjectId()
    {
        var (service, logger) = CreateService();
        using var capture = CreateCapture();

        var request = new RectificationRequest(SubjectId, "Email", "new@example.com", null, null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleRectificationAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandleRestrictionAsync_NeverCarriesSubjectId()
    {
        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

        var (service, logger) = CreateService(repository: repository);
        using var capture = CreateCapture();

        var request = new RestrictionRequest(SubjectId, "Accuracy contested", Scope: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleRestrictionAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandlePortabilityAsync_NeverCarriesSubjectId()
    {
        var portabilityExporter = Substitute.For<IDataPortabilityExporter>();
        portabilityExporter.ExportAsync(SubjectId, ExportFormat.JSON, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, PortabilityResponse>(new PortabilityResponse
            {
                SubjectId = SubjectId,
                ExportedData = new ExportedData
                {
                    Content = [],
                    ContentType = "application/json",
                    FileName = "export.json",
                    Format = ExportFormat.JSON,
                    FieldCount = 0
                },
                GeneratedAtUtc = DateTimeOffset.UtcNow
            }));

        var (service, logger) = CreateService(portabilityExporter: portabilityExporter);
        using var capture = CreateCapture();

        var request = new PortabilityRequest(SubjectId, ExportFormat.JSON, Categories: null);
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandlePortabilityAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task HandleObjectionAsync_NeverCarriesSubjectId()
    {
        var (service, logger) = CreateService();
        using var capture = CreateCapture();

        var request = new ObjectionRequest(SubjectId, "Marketing", "I object");
        var result = await DiagnosticsCapture.CaptureAsync(() => service.HandleObjectionAsync(request).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    private static (DefaultDSRService Service, FakeLogger<DefaultDSRService> Logger) CreateService(
        IAggregateRepository<DSRRequestAggregate>? repository = null,
        IPersonalDataLocator? locator = null,
        IDataErasureExecutor? erasureExecutor = null,
        IDataPortabilityExporter? portabilityExporter = null)
    {
        var logger = new FakeLogger<DefaultDSRService>();
        var service = new DefaultDSRService(
            repository ?? Substitute.For<IAggregateRepository<DSRRequestAggregate>>(),
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            locator ?? Substitute.For<IPersonalDataLocator>(),
            erasureExecutor ?? Substitute.For<IDataErasureExecutor>(),
            portabilityExporter ?? Substitute.For<IDataPortabilityExporter>(),
            Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(),
            logger);
        return (service, logger);
    }

    #endregion

    #region DefaultDataErasureExecutor

    [Fact]
    public async Task EraseAsync_NoDataFound_NeverCarriesSubjectId()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                (IReadOnlyList<PersonalDataLocation>)new List<PersonalDataLocation>())));
        var strategy = Substitute.For<IDataErasureStrategy>();

        var logger = new FakeLogger<DefaultDataErasureExecutor>();
        var sut = new DefaultDataErasureExecutor(locator, strategy, logger);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => sut.EraseAsync(SubjectId, new ErasureScope { Reason = ErasureReason.NoLongerNecessary }).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task EraseAsync_LocatorFails_NeverCarriesSubjectId()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, IReadOnlyList<PersonalDataLocation>>(
                DSRErrors.ServiceError("LocateAllData", new InvalidOperationException("boom")))));
        var strategy = Substitute.For<IDataErasureStrategy>();

        var logger = new FakeLogger<DefaultDataErasureExecutor>();
        var sut = new DefaultDataErasureExecutor(locator, strategy, logger);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => sut.EraseAsync(SubjectId, new ErasureScope { Reason = ErasureReason.NoLongerNecessary }).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    #endregion

    #region DefaultDataPortabilityExporter

    [Fact]
    public async Task ExportAsync_UnsupportedFormat_NeverCarriesSubjectId()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        var logger = new FakeLogger<DefaultDataPortabilityExporter>();
        var sut = new DefaultDataPortabilityExporter(locator, [], new FakeTimeProvider(), logger);
        using var capture = CreateCapture();

        var result = await DiagnosticsCapture.CaptureAsync(
            () => sut.ExportAsync(SubjectId, ExportFormat.JSON).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    #endregion

    #region ProcessingRestrictionPipelineBehavior

    [RestrictProcessing(SubjectIdProperty = nameof(SubjectIdValue))]
    private sealed record RestrictedCommand(string SubjectIdValue) : IRequest<Unit>;

    [Fact]
    public async Task Handle_Blocked_NeverCarriesSubjectId()
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var behavior = CreateBehavior(dsrService, out var logger, DSREnforcementMode.Block);
        using var capture = CreateCapture();

        var command = new RestrictedCommand(SubjectId);
        var result = await DiagnosticsCapture.CaptureAsync(() => behavior.Handle(
            command, Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, Unit>(unit)), CancellationToken.None).AsTask());

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task Handle_Warned_NeverCarriesSubjectId()
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        var behavior = CreateBehavior(dsrService, out var logger, DSREnforcementMode.Warn);
        using var capture = CreateCapture();

        var command = new RestrictedCommand(SubjectId);
        var result = await DiagnosticsCapture.CaptureAsync(() => behavior.Handle(
            command, Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, Unit>(unit)), CancellationToken.None).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    [Fact]
    public async Task Handle_StoreError_NeverCarriesSubjectId()
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(SubjectId, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(DSRErrors.ServiceError("HasActiveRestriction", new InvalidOperationException("boom"))));

        var behavior = CreateBehavior(dsrService, out var logger, DSREnforcementMode.Block);
        using var capture = CreateCapture();

        var command = new RestrictedCommand(SubjectId);
        var result = await DiagnosticsCapture.CaptureAsync(() => behavior.Handle(
            command, Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult(Right<EncinaError, Unit>(unit)), CancellationToken.None).AsTask());

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: true);
    }

    private static ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit> CreateBehavior(
        IDSRService dsrService,
        out FakeLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>> logger,
        DSREnforcementMode mode)
    {
        var options = Options.Create(new DataSubjectRightsOptions { RestrictionEnforcementMode = mode });
        logger = new FakeLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();
        return new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, Substitute.For<IDataSubjectIdExtractor>(), options, logger);
    }

    #endregion

    private static DiagnosticsCapture CreateCapture() =>
        new(DataSubjectRightsDiagnostics.ActivitySource, DataSubjectRightsDiagnostics.Meter);

    /// <summary>
    /// Captures every log record, activity and metric tag recorded on a given
    /// <see cref="ActivitySource"/>/<see cref="Meter"/> pair, but only for the calls made through
    /// <see cref="DiagnosticsCapture.CaptureAsync{T}"/> — an <see cref="AsyncLocal{T}"/> flag scopes
    /// capture to this test's own logical call, so a process-global listener never picks up another
    /// test's concurrently running activity on the same shared source (#1429; see #1423, where a
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

        public void AssertNoSubjectId<T>(FakeLogger<T> logger, string subjectId, bool expectActivity)
        {
            var logs = logger.Collector.GetSnapshot();
            logs.ShouldAllBe(r => !r.Message.Contains(subjectId, StringComparison.Ordinal));

            List<Activity> activitiesSnapshot;
            List<object?> tagValuesSnapshot;
            lock (_sync)
            {
                activitiesSnapshot = [.. _activities];
                tagValuesSnapshot = [.. _tagValues];
            }

            if (expectActivity)
            {
                activitiesSnapshot.ShouldNotBeEmpty();
            }

            foreach (var activity in activitiesSnapshot)
            {
                activity.DisplayName.ShouldNotContain(subjectId);
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(subjectId);
            }

            foreach (var tagValue in tagValuesSnapshot)
            {
                if (tagValue is string tagString)
                {
                    tagString.ShouldNotContain(subjectId);
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
