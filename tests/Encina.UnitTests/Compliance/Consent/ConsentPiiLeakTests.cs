using System.Diagnostics;
using System.Diagnostics.Metrics;

using Encina.Caching;
using Encina.Compliance.Consent;
using Encina.Compliance.Consent.Aggregates;
using Encina.Compliance.Consent.Diagnostics;
using Encina.Compliance.Consent.ReadModels;
using Encina.Compliance.Consent.Services;
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

namespace Encina.UnitTests.Compliance.Consent;

/// <summary>
/// The data subject's own identifier must never reach a log message, an OpenTelemetry activity
/// tag/status, a metric tag, or <see cref="EncinaError"/> metadata produced by
/// <c>Encina.Compliance.Consent</c> (#1314). Modelled on
/// <c>Encina.UnitTests.Messaging.Encryption.ErrorMessageLeakTests</c>.
/// </summary>
#pragma warning disable CA2012 // Use ValueTasks correctly (NSubstitute Returns with ValueTask)
public sealed class ConsentPiiLeakTests
{
    private const string SubjectId = "patient-42";

    #region Pipeline behavior — logs, activity tags, metric tags

    [Fact]
    public async Task PipelineBehavior_ValidConsent_NeverCarriesSubjectId()
    {
        var validator = Substitute.For<IConsentValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(ConsentValidationResult.Valid()));

        var behavior = CreateBehavior(validator, ConsentEnforcementMode.Block, out var logger);
        using var capture = new DiagnosticsCapture();

        var context = RequestContext.CreateForTest(userId: SubjectId);
        var result = await behavior.Handle(
            new PiiRequest(SubjectId), context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId);
    }

    [Fact]
    public async Task PipelineBehavior_MissingConsent_BlockMode_NeverCarriesSubjectId()
    {
        var validator = Substitute.For<IConsentValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Invalid(["Missing consent"], [ConsentPurposes.Marketing])));

        var behavior = CreateBehavior(validator, ConsentEnforcementMode.Block, out var logger);
        using var capture = new DiagnosticsCapture();

        var context = RequestContext.CreateForTest(userId: SubjectId);
        var result = await behavior.Handle(
            new PiiRequest(SubjectId), context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId);
    }

    [Fact]
    public async Task PipelineBehavior_MissingConsent_WarnMode_NeverCarriesSubjectId()
    {
        var validator = Substitute.For<IConsentValidator>();
        validator.ValidateAsync(Arg.Any<string>(), Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, ConsentValidationResult>>(
                ConsentValidationResult.Invalid(["Missing consent"], [ConsentPurposes.Marketing])));

        var behavior = CreateBehavior(validator, ConsentEnforcementMode.Warn, out var logger);
        using var capture = new DiagnosticsCapture();

        var context = RequestContext.CreateForTest(userId: SubjectId);
        var result = await behavior.Handle(
            new PiiRequest(SubjectId), context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId);
    }

    [Fact]
    public async Task PipelineBehavior_SubjectIdUnresolved_NeverCarriesSubjectId()
    {
        var validator = Substitute.For<IConsentValidator>();
        var behavior = CreateBehavior(validator, ConsentEnforcementMode.Block, out var logger);
        using var capture = new DiagnosticsCapture();

        // No SubjectIdProperty is configured on PiiRequest and the context has no UserId, so the
        // subject cannot be resolved — the check must fail closed without ever having logged the
        // (unresolved) subject id.
        var context = RequestContext.CreateForTest(userId: null);
        var result = await behavior.Handle(
            new PiiRequest(SubjectId), context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default), CancellationToken.None);

        // The subject-unresolved path returns before ConsentDiagnostics.StartConsentCheck runs
        // (see ConsentRequiredPipelineBehavior.Handle), so no activity is ever started for it.
        result.IsLeft.ShouldBeTrue();
        capture.AssertNoSubjectId(logger, SubjectId, expectActivity: false);
    }

    private static ConsentRequiredPipelineBehavior<PiiRequest, Unit> CreateBehavior(
        IConsentValidator validator,
        ConsentEnforcementMode mode,
        out FakeLogger<ConsentRequiredPipelineBehavior<PiiRequest, Unit>> logger)
    {
        var options = new ConsentOptions();
        options.DefinePurpose(ConsentPurposes.Marketing);
        options.EnforcementMode = mode;
        logger = new FakeLogger<ConsentRequiredPipelineBehavior<PiiRequest, Unit>>();
        return new ConsentRequiredPipelineBehavior<PiiRequest, Unit>(validator, Options.Create(options), logger);
    }

    #endregion

    #region DefaultConsentService

    [Fact]
    public async Task ConsentService_GrantConsent_NeverLogsSubjectId()
    {
        var repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
        repository.CreateAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var (service, logger) = CreateService(repository: repository);

        // grantedBy is deliberately the data subject's own id — the realistic self-service case
        // where the actor and the subject are the same person — so this proves the actor field
        // does not become a side-channel for the identifier the log message itself never prints.
        var result = await service.GrantConsentAsync(SubjectId, "marketing", "v1", "web-form", SubjectId);

        result.IsRight.ShouldBeTrue();
        AssertLogsNeverContain(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentService_WithdrawConsent_NeverLogsActorId()
    {
        var repository = Substitute.For<IAggregateRepository<ConsentAggregate>>();
        var aggregate = ConsentAggregate.Grant(
            Guid.NewGuid(), SubjectId, "marketing", "v1", "web-form",
            null, null, new Dictionary<string, object?>(), null, SubjectId, DateTimeOffset.UtcNow, null, null);
        repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ConsentAggregate>(aggregate));
        repository.SaveAsync(Arg.Any<ConsentAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));

        var (service, logger) = CreateService(repository: repository);

        // withdrawnBy is deliberately the data subject's own id, same rationale as above.
        var result = await service.WithdrawConsentAsync(aggregate.Id, SubjectId);

        result.IsRight.ShouldBeTrue();
        AssertLogsNeverContain(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentService_GetConsentBySubjectAndPurpose_NeverLogsSubjectId()
    {
        var readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
        readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(new List<ConsentReadModel>()));

        var (service, logger) = CreateService(readModelRepository: readModelRepository);

        var result = await service.GetConsentBySubjectAndPurposeAsync(SubjectId, "marketing");

        result.IsRight.ShouldBeTrue();
        AssertLogsNeverContain(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentService_GetAllConsents_NeverLogsSubjectId()
    {
        var readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
        readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(new List<ConsentReadModel>()));

        var (service, logger) = CreateService(readModelRepository: readModelRepository);

        var result = await service.GetAllConsentsAsync(SubjectId);

        result.IsRight.ShouldBeTrue();
        AssertLogsNeverContain(logger, SubjectId);
    }

    [Fact]
    public async Task ConsentService_HasValidConsent_NeverLogsSubjectId()
    {
        var readModelRepository = Substitute.For<IReadModelRepository<ConsentReadModel>>();
        readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<ConsentReadModel>, IQueryable<ConsentReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<ConsentReadModel>>(new List<ConsentReadModel>()));

        var (service, logger) = CreateService(readModelRepository: readModelRepository);

        var result = await service.HasValidConsentAsync(SubjectId, "marketing");

        result.IsRight.ShouldBeTrue();
        AssertLogsNeverContain(logger, SubjectId);
    }

    private static (DefaultConsentService Service, FakeLogger<DefaultConsentService> Logger) CreateService(
        IAggregateRepository<ConsentAggregate>? repository = null,
        IReadModelRepository<ConsentReadModel>? readModelRepository = null)
    {
        var logger = new FakeLogger<DefaultConsentService>();
        var service = new DefaultConsentService(
            repository ?? Substitute.For<IAggregateRepository<ConsentAggregate>>(),
            readModelRepository ?? Substitute.For<IReadModelRepository<ConsentReadModel>>(),
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(),
            Substitute.For<IRequestContextAccessor>(),
            Options.Create(new ConsentOptions()),
            logger);
        return (service, logger);
    }

    private static void AssertLogsNeverContain(FakeLogger logger, string forbidden)
    {
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldAllBe(r => !r.Message.Contains(forbidden, StringComparison.Ordinal));
    }

    #endregion

    #region ConsentErrors metadata

    [Fact]
    public void MissingConsent_DetailsNeverCarrySubjectId()
    {
        var error = ConsentErrors.MissingConsent(SubjectId, "marketing");
        AssertNoSubjectIdInDetails(error, SubjectId);
    }

    [Fact]
    public void ConsentExpired_DetailsNeverCarrySubjectId()
    {
        var error = ConsentErrors.ConsentExpired(SubjectId, "marketing", DateTimeOffset.UtcNow);
        AssertNoSubjectIdInDetails(error, SubjectId);
    }

    [Fact]
    public void ConsentWithdrawn_DetailsNeverCarrySubjectId()
    {
        var error = ConsentErrors.ConsentWithdrawn(SubjectId, "marketing", DateTimeOffset.UtcNow);
        AssertNoSubjectIdInDetails(error, SubjectId);
    }

    [Fact]
    public void RequiresReconsent_DetailsNeverCarrySubjectId()
    {
        var error = ConsentErrors.RequiresReconsent(SubjectId, "marketing", "v2", "v1");
        AssertNoSubjectIdInDetails(error, SubjectId);
    }

    [Fact]
    public void VersionMismatch_DetailsNeverCarrySubjectId()
    {
        var error = ConsentErrors.VersionMismatch(SubjectId, "marketing", "v2", "v1");
        AssertNoSubjectIdInDetails(error, SubjectId);
    }

    private static void AssertNoSubjectIdInDetails(EncinaError error, string subjectId)
    {
        var details = error.GetDetails();
        details.ContainsKey("subjectId").ShouldBeFalse();
        foreach (var value in details.Values)
        {
            (value?.ToString() ?? string.Empty).ShouldNotContain(subjectId);
        }
    }

    #endregion

    [RequireConsent(ConsentPurposes.Marketing)]
    public sealed record PiiRequest(string SubjectId) : ICommand<Unit>;

    /// <summary>
    /// Captures every tag recorded on the Consent ActivitySource and Meter for the duration of the
    /// scope, so a test can assert the data subject's own identifier never appears in them.
    /// </summary>
    private sealed class DiagnosticsCapture : IDisposable
    {
        private readonly ActivityListener _activityListener;
        private readonly MeterListener _meterListener;
        private readonly List<Activity> _activities = [];
        private readonly List<object?> _tagValues = [];

        public DiagnosticsCapture()
        {
            _activityListener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ConsentDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                {
                    _activities.Add(activity);
                    foreach (var tag in activity.Tags)
                    {
                        _tagValues.Add(tag.Value);
                    }
                }
            };
            ActivitySource.AddActivityListener(_activityListener);

            _meterListener = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == ConsentDiagnostics.SourceName)
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
            foreach (var tag in tags)
            {
                _tagValues.Add(tag.Value);
            }
        }

        public void AssertNoSubjectId(FakeLogger logger, string subjectId, bool expectActivity = true)
        {
            var logs = logger.Collector.GetSnapshot();
            logs.ShouldAllBe(r => !r.Message.Contains(subjectId, StringComparison.Ordinal));

            if (expectActivity)
            {
                _activities.ShouldNotBeEmpty();
            }

            // Snapshot before iterating: ActivityStopped/measurement callbacks can still append to
            // the backing lists from another thread while this assertion runs.
            foreach (var activity in _activities.ToArray())
            {
                activity.DisplayName.ShouldNotContain(subjectId);
                (activity.StatusDescription ?? string.Empty).ShouldNotContain(subjectId);
            }

            foreach (var tagValue in _tagValues.ToArray())
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
