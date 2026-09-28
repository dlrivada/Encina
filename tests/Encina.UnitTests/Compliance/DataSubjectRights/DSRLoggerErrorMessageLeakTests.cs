#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Caching;
using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.DataSubjectRights.Aggregates;
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
/// <see cref="EncinaError.Message"/> and a caught exception's own <c>Message</c> must never reach a
/// logger call in <c>Encina.Compliance.DataSubjectRights</c> (#1505, extending #1503's DPIA rule to
/// this sibling package). Only <c>error.GetCode().IfNone("encina.unknown")</c> (for an
/// <see cref="EncinaError"/>) or <c>ex.GetType().Name</c> (for a caught exception) may reach the
/// logger. One test per distinct logger method that was fixed; each fails on the pre-#1505 code
/// (the sentinel string used to appear in the log record) and passes on the fixed code.
/// </summary>
public sealed class DSRLoggerErrorMessageLeakTests
{
    // A sentinel that must never appear in any log record. Chosen to be extremely unlikely to
    // collide with any error code, exception type name or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-DSR-LOG-MESSAGE-do-not-log-this-9c31fa";

    private static readonly EncinaError SensitiveError =
        EncinaErrors.Create("dsr.store_error", SentinelMessage);

    #region DefaultDSRService — DSRServiceError (caught exception)

    [Fact]
    public async Task SubmitRequestAsync_RepositoryThrows_LogsOnlyTheExceptionType()
    {
        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.CreateAsync(Arg.Any<DSRRequestAggregate>(), Arg.Any<CancellationToken>())
            .Returns<Task<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException(SentinelMessage));

        var logger = new FakeLogger<DefaultDSRService>();
        var sut = CreateService(repository: repository, logger: logger);

        var result = await sut.SubmitRequestAsync("subject-1", DataSubjectRight.Access);

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("InvalidOperationException"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region DefaultDSRService — DSRInvalidStateTransition (caught exception)

    [Fact]
    public async Task VerifyIdentityAsync_AggregateThrowsInvalidOperation_LogsOnlyTheExceptionType()
    {
        var aggregate = DSRRequestAggregate.Submit(
            Guid.NewGuid(), "subject-1", DataSubjectRight.Access, DateTimeOffset.UtcNow);
        aggregate.Verify("verifier", DateTimeOffset.UtcNow);

        var repository = Substitute.For<IAggregateRepository<DSRRequestAggregate>>();
        repository.LoadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, DSRRequestAggregate>(aggregate)));

        var logger = new FakeLogger<DefaultDSRService>();
        var sut = CreateService(repository: repository, logger: logger);

        // Verifying an already-verified aggregate throws InvalidOperationException from the aggregate.
        var result = await sut.VerifyIdentityAsync(Guid.NewGuid(), "verifier-2");

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("InvalidOperationException"));
    }

    #endregion

    #region DefaultDSRService.HandleAccessAsync — AccessProcessingActivitiesFailed, DSRRequestFailed

    [Fact]
    public async Task HandleAccessAsync_ProcessingActivitiesFail_LogsOnlyTheErrorCode()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(
                new List<PersonalDataLocation>()));

        var registry = Substitute.For<IProcessingActivityRegistry>();
        registry.GetAllActivitiesAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<ProcessingActivity>>>(SensitiveError));

        var logger = new FakeLogger<DefaultDSRService>();
        var sut = CreateService(locator: locator, registry: registry, logger: logger);

        var request = new AccessRequest("subject-1", IncludeProcessingActivities: true);
        var result = await sut.HandleAccessAsync(request);

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task HandleAccessAsync_LocatorFails_LogsOnlyTheErrorCode()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(SensitiveError));

        var logger = new FakeLogger<DefaultDSRService>();
        var sut = CreateService(locator: locator, logger: logger);

        var request = new AccessRequest("subject-1", IncludeProcessingActivities: false);
        var result = await sut.HandleAccessAsync(request);

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region DefaultDataErasureExecutor — ErasureFailed, ErasureFieldFailed

    [Fact]
    public async Task EraseAsync_LocatorFails_LogsOnlyTheErrorCode()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(SensitiveError));
        var strategy = Substitute.For<IDataErasureStrategy>();
        var logger = new FakeLogger<DefaultDataErasureExecutor>();
        var sut = new DefaultDataErasureExecutor(locator, strategy, logger);

        var result = await sut.EraseAsync("subject-1", new ErasureScope { Reason = ErasureReason.ConsentWithdrawn });

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task EraseAsync_StrategyFailsForAField_LogsOnlyTheErrorCode()
    {
        var location = new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(
                new List<PersonalDataLocation> { location }));

        var strategy = Substitute.For<IDataErasureStrategy>();
        strategy.EraseFieldAsync(Arg.Any<PersonalDataLocation>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(SensitiveError));

        var logger = new FakeLogger<DefaultDataErasureExecutor>();
        var sut = new DefaultDataErasureExecutor(locator, strategy, logger);

        var result = await sut.EraseAsync("subject-1", new ErasureScope { Reason = ErasureReason.ConsentWithdrawn });

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region DefaultDataPortabilityExporter — PortabilityExportFailed

    [Fact]
    public async Task ExportAsync_LocatorFails_LogsOnlyTheErrorCode()
    {
        var locator = Substitute.For<IPersonalDataLocator>();
        locator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(SensitiveError));

        var writer = Substitute.For<IExportFormatWriter>();
        writer.SupportedFormat.Returns(ExportFormat.JSON);

        var logger = new FakeLogger<DefaultDataPortabilityExporter>();
        var sut = new DefaultDataPortabilityExporter(
            locator, [writer], new FakeTimeProvider(), logger);

        var result = await sut.ExportAsync("subject-1", ExportFormat.JSON);

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region DefaultDSRService.HandleRectificationAsync (via PublishNotificationAsync) — NotificationPublishFailed

    [Fact]
    public async Task HandleRectificationAsync_PublishFails_LogsOnlyTheErrorCode()
    {
        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Unit>>(SensitiveError));

        var logger = new FakeLogger<DefaultDSRService>();
        var sut = CreateService(logger: logger, encina: encina);

        var request = new RectificationRequest("subject-1", "Email", "new@example.com", EntityType: null, EntityId: null);
        var result = await sut.HandleRectificationAsync(request);

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region ProcessingRestrictionPipelineBehavior — RestrictionCheckStoreError

    [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
    private sealed record RestrictedCommand(string CustomerId) : IRequest<Unit>;

    [Fact]
    public async Task Handle_RestrictionStoreLookupFails_LogsOnlyTheErrorCode()
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, bool>>(SensitiveError));

        var options = Options.Create(new DataSubjectRightsOptions { RestrictionEnforcementMode = DSREnforcementMode.Warn });
        var logger = new FakeLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var sut = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(dsrService, extractor, options, logger);
        var context = Substitute.For<IRequestContext>();

        var result = await sut.Handle(
            new RestrictedCommand("customer-1"),
            context,
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(unit),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region CompositePersonalDataLocator — inline LogWarning

    [Fact]
    public async Task LocateAllDataAsync_OneLocatorFails_LogsOnlyTheErrorCode()
    {
        var failingLocator = Substitute.For<IPersonalDataLocator>();
        failingLocator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(SensitiveError));

        var location = new PersonalDataLocation
        {
            EntityType = typeof(object),
            EntityId = "entity-1",
            FieldName = "Email",
            Category = PersonalDataCategory.Contact,
            IsErasable = true,
            IsPortable = false,
            HasLegalRetention = false
        };

        var succeedingLocator = Substitute.For<IPersonalDataLocator>();
        succeedingLocator.LocateAllDataAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(
                new List<PersonalDataLocation> { location }));

        var logger = new FakeLogger<CompositePersonalDataLocator>();
        var sut = new CompositePersonalDataLocator([failingLocator, succeedingLocator], logger);

        var result = await sut.LocateAllDataAsync("subject-1");

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("dsr.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    private static DefaultDSRService CreateService(
        IAggregateRepository<DSRRequestAggregate>? repository = null,
        IPersonalDataLocator? locator = null,
        IProcessingActivityRegistry? registry = null,
        ILogger<DefaultDSRService>? logger = null,
        IEncina? encina = null)
    {
        return new DefaultDSRService(
            repository ?? Substitute.For<IAggregateRepository<DSRRequestAggregate>>(),
            Substitute.For<IReadModelRepository<DSRRequestReadModel>>(),
            locator ?? Substitute.For<IPersonalDataLocator>(),
            Substitute.For<IDataErasureExecutor>(),
            Substitute.For<IDataPortabilityExporter>(),
            registry ?? Substitute.For<IProcessingActivityRegistry>(),
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(),
            logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultDSRService>.Instance,
            encina);
    }
}
