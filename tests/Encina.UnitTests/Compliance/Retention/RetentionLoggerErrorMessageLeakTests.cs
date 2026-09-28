#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Caching;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Aggregates;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using Encina.Compliance.Retention.Services;
using Encina.Marten;
using Encina.Marten.Projections;

using LanguageExt;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// <see cref="EncinaError.Message"/> and a caught exception's own <c>Message</c> must never reach a
/// logger call in <c>Encina.Compliance.Retention</c> (#1505, extending #1503's DPIA rule to this
/// sibling package). Only <c>error.GetCode().IfNone("encina.unknown")</c> (for an
/// <see cref="EncinaError"/>) or <c>ex.GetType().Name</c> (for a caught exception) may reach the
/// logger. One test per distinct logger method that was fixed; each fails on the pre-#1505 code
/// (the sentinel string used to appear in the log record) and passes on the fixed code.
/// </summary>
public sealed class RetentionLoggerErrorMessageLeakTests
{
    // A sentinel that must never appear in any log record. Chosen to be extremely unlikely to
    // collide with any error code, exception type name or fixed outcome string used by the
    // production code under test.
    private const string SentinelMessage = "SENTINEL-RETENTION-LOG-MESSAGE-do-not-log-this-2be47d";

    private static readonly EncinaError SensitiveError =
        EncinaErrors.Create("retention.store_error", SentinelMessage);

    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    #region RetentionValidationPipelineBehavior — RetentionRecordCreationBlocked / RetentionRecordCreationWarned

    [RetentionPeriod(Days = 365, DataCategory = "test-category", Reason = "Test")]
    private sealed record DecoratedResponse
    {
        public string Id { get; init; } = string.Empty;
    }

    private sealed record DecoratedCommand : IRequest<DecoratedResponse>;

    [Fact]
    public async Task Handle_BlockMode_TrackEntityReturnsError_LogsOnlyTheErrorCode()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.TrackEntityAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(SensitiveError));

        var logger = new FakeLogger<RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>>();
        var behavior = new RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            recordService,
            Substitute.For<IRetentionPolicyService>(),
            Options.Create(new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Block }),
            logger);

        var response = new DecoratedResponse { Id = "entity-42" };
        var result = await behavior.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task Handle_WarnMode_TrackEntityReturnsError_LogsOnlyTheErrorCode()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.TrackEntityAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<TimeSpan>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(SensitiveError));

        var logger = new FakeLogger<RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>>();
        var behavior = new RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            recordService,
            Substitute.For<IRetentionPolicyService>(),
            Options.Create(new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Warn }),
            logger);

        var response = new DecoratedResponse { Id = "entity-42" };
        var result = await behavior.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    // The following two tests exercise HandleMissingEntityId (extracted from
    // TrackRetentionRecordAsync by the #1505 CRAP-gate refactor): they do not touch the
    // EncinaError.Message sink rule, but a whitespace-only resolved entity ID is a real, reachable
    // production path (unlike the policy-service lookup branch of ResolveRetentionPeriodAsync, which
    // ResolveAttributeInfo's `RetentionPeriod > TimeSpan.Zero` filter makes unreachable through the
    // normal attribute scan) and was otherwise untested, pushing HandleMissingEntityId's CRAP score
    // above the gate.

    [Fact]
    public async Task Handle_BlockMode_EmptyEntityId_ReturnsBlockingError()
    {
        var logger = new FakeLogger<RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>>();
        var behavior = new RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            Substitute.For<IRetentionRecordService>(),
            Substitute.For<IRetentionPolicyService>(),
            Options.Create(new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Block }),
            logger);

        var response = new DecoratedResponse { Id = string.Empty };
        var result = await behavior.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_WhitespaceEntityId_AllowsResponseThrough()
    {
        var logger = new FakeLogger<RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>>();
        var behavior = new RetentionValidationPipelineBehavior<DecoratedCommand, DecoratedResponse>(
            Substitute.For<IRetentionRecordService>(),
            Substitute.For<IRetentionPolicyService>(),
            Options.Create(new RetentionOptions { EnforcementMode = RetentionEnforcementMode.Warn }),
            logger);

        var response = new DecoratedResponse { Id = "   " };
        var result = await behavior.Handle(
            new DecoratedCommand(),
            Substitute.For<IRequestContext>(),
            () => new ValueTask<Either<EncinaError, DecoratedResponse>>(Right<EncinaError, DecoratedResponse>(response)),
            CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((DecoratedResponse)result).Id.ShouldBe("   ");
    }

    #endregion

    #region RetentionEnforcementService — RetentionEnforcementCycleFailed(string), RetentionEnforcementTransitionFailed, RetentionSiblingCheckFailed, RetentionLegalHoldCheckFailed

    private static RetentionEnforcementService CreateEnforcementSut(
        IRetentionRecordService recordService,
        ILegalHoldService legalHoldService,
        FakeLogger<RetentionEnforcementService> logger,
        IRetentionDataEraser? dataEraser = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(recordService);
        services.AddSingleton(legalHoldService);
        if (dataEraser is not null)
        {
            services.AddSingleton(dataEraser);
        }

        var provider = services.BuildServiceProvider();

        return new RetentionEnforcementService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new RetentionOptions
            {
                EnableAutomaticEnforcement = true,
                EnforcementInterval = TimeSpan.FromHours(1),
                PublishNotifications = false
            }),
            logger,
            new FakeTimeProvider(Now));
    }

    private static RetentionRecordReadModel Record(string entityId, RetentionStatus status = RetentionStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        DataCategory = "test-data",
        ExpiresAtUtc = Now.AddDays(-1),
        Status = status
    };

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_GetExpiredRecordsReturnsError_LogsOnlyTheErrorCode()
    {
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(SensitiveError));

        var logger = new FakeLogger<RetentionEnforcementService>();
        var sut = CreateEnforcementSut(recordService, Substitute.For<ILegalHoldService>(), logger);

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_MarkExpiredReturnsError_LogsOnlyTheErrorCode()
    {
        var record = Record("entity-1");
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>([record]));
        recordService.GetRecordsByEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(System.Array.Empty<RetentionRecordReadModel>()));
        recordService.MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(SensitiveError));

        var legalHoldService = Substitute.For<ILegalHoldService>();
        legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        var logger = new FakeLogger<RetentionEnforcementService>();
        var sut = CreateEnforcementSut(recordService, legalHoldService, logger);

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_GetRecordsByEntityThrows_LogsOnlyTheExceptionType()
    {
        var record = Record("entity-1", RetentionStatus.Expired);
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>([record]));
        recordService.GetRecordsByEntityAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, IReadOnlyList<RetentionRecordReadModel>>>>(
                _ => throw new InvalidOperationException(SentinelMessage));

        var legalHoldService = Substitute.For<ILegalHoldService>();
        legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

        // A data eraser must be registered, otherwise the record is reported ErasureUnavailable
        // before the sibling lookup below ever runs.
        var dataEraser = Substitute.For<IRetentionDataEraser>();

        var logger = new FakeLogger<RetentionEnforcementService>();
        var sut = CreateEnforcementSut(recordService, legalHoldService, logger, dataEraser);

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("InvalidOperationException"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task ExecuteEnforcementCycleAsync_HasActiveHoldsReturnsError_LogsOnlyTheErrorCode()
    {
        var record = Record("entity-1");
        var recordService = Substitute.For<IRetentionRecordService>();
        recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>([record]));

        var legalHoldService = Substitute.For<ILegalHoldService>();
        legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(SensitiveError));

        var logger = new FakeLogger<RetentionEnforcementService>();
        var sut = CreateEnforcementSut(recordService, legalHoldService, logger);

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region RetentionAutoRegistrationHostedService / RetentionFluentPolicyHostedService — RetentionAutoRegistrationPolicyFailed(string)

    [RetentionPeriod(Days = 30, DataCategory = "auto-category", Reason = "Test")]
    private sealed class AutoRegisteredEntity;

    [Fact]
    public async Task AutoRegistration_CreatePolicyReturnsError_LogsOnlyTheErrorCode()
    {
        var policyService = Substitute.For<IRetentionPolicyService>();
        policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, RetentionPolicyReadModel>>(
                RetentionErrors.PolicyNotFound("auto-category")));
        policyService.CreatePolicyAsync(
                dataCategory: Arg.Any<string>(),
                retentionPeriod: Arg.Any<TimeSpan>(),
                autoDelete: Arg.Any<bool>(),
                policyType: Arg.Any<RetentionPolicyType>(),
                reason: Arg.Any<string?>(),
                legalBasis: Arg.Any<string?>(),
                tenantId: Arg.Any<string?>(),
                moduleId: Arg.Any<string?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(SensitiveError));

        var services = new ServiceCollection();
        services.AddSingleton(policyService);
        var provider = services.BuildServiceProvider();

        var logger = new FakeLogger<RetentionAutoRegistrationHostedService>();
        var descriptor = new RetentionAutoRegistrationDescriptor([typeof(AutoRegisteredEntity).Assembly]);
        var sut = new RetentionAutoRegistrationHostedService(
            descriptor,
            Options.Create(new RetentionOptions { AutoRegisterFromAttributes = true }),
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);

        await sut.StartAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task FluentPolicy_CreatePolicyReturnsError_LogsOnlyTheErrorCode()
    {
        var policyService = Substitute.For<IRetentionPolicyService>();
        policyService.GetPolicyByCategoryAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, RetentionPolicyReadModel>>(
                RetentionErrors.PolicyNotFound("fluent-category")));
        policyService.CreatePolicyAsync(
                dataCategory: Arg.Any<string>(),
                retentionPeriod: Arg.Any<TimeSpan>(),
                autoDelete: Arg.Any<bool>(),
                policyType: Arg.Any<RetentionPolicyType>(),
                reason: Arg.Any<string?>(),
                legalBasis: Arg.Any<string?>(),
                tenantId: Arg.Any<string?>(),
                moduleId: Arg.Any<string?>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<Either<EncinaError, Guid>>(SensitiveError));

        var services = new ServiceCollection();
        services.AddSingleton(policyService);
        var provider = services.BuildServiceProvider();

        var descriptor = new RetentionFluentPolicyDescriptor(
            [new RetentionPolicyDescriptor("fluent-category", TimeSpan.FromDays(30), true, "Test", null)]);

        var logger = new FakeLogger<RetentionFluentPolicyHostedService>();
        var sut = new RetentionFluentPolicyHostedService(
            descriptor,
            provider.GetRequiredService<IServiceScopeFactory>(),
            logger);

        await sut.StartAsync(CancellationToken.None);

        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion

    #region DefaultLegalHoldService — LegalHoldCascadeFailed, LegalHoldOtherHoldsCheckFailed, LegalHoldRecordHoldFailed, LegalHoldRecordReleaseFailed

    private static DefaultLegalHoldService CreateLegalHoldSut(
        IReadModelRepository<LegalHoldReadModel> readModelRepository,
        IReadModelRepository<RetentionRecordReadModel> recordReadModelRepository,
        IRetentionRecordService retentionRecordService,
        FakeLogger<DefaultLegalHoldService> logger,
        IAggregateRepository<LegalHoldAggregate>? repository = null) =>
        new(
            repository ?? Substitute.For<IAggregateRepository<LegalHoldAggregate>>(),
            readModelRepository,
            recordReadModelRepository,
            retentionRecordService,
            Substitute.For<ICacheProvider>(),
            new FakeTimeProvider(Now),
            logger);

    [Fact]
    public async Task PlaceHoldAsync_RecordQueryReturnsError_LogsOnlyTheErrorCode()
    {
        var repository = Substitute.For<IAggregateRepository<LegalHoldAggregate>>();
        repository.CreateAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var recordReadModelRepository = Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();
        recordReadModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<RetentionRecordReadModel>, IQueryable<RetentionRecordReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(SensitiveError)));

        var logger = new FakeLogger<DefaultLegalHoldService>();
        var sut = CreateLegalHoldSut(
            Substitute.For<IReadModelRepository<LegalHoldReadModel>>(),
            recordReadModelRepository,
            Substitute.For<IRetentionRecordService>(),
            logger,
            repository);

        var result = await sut.PlaceHoldAsync("entity-1", "reason", "applier-1");

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task PlaceHoldAsync_HoldRecordReturnsError_LogsOnlyTheErrorCode()
    {
        var repository = Substitute.For<IAggregateRepository<LegalHoldAggregate>>();
        repository.CreateAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var record = Record("entity-1");
        var recordReadModelRepository = Substitute.For<IReadModelRepository<RetentionRecordReadModel>>();
        recordReadModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<RetentionRecordReadModel>, IQueryable<RetentionRecordReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>([record])));

        var retentionRecordService = Substitute.For<IRetentionRecordService>();
        retentionRecordService.HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(SensitiveError));

        var logger = new FakeLogger<DefaultLegalHoldService>();
        var sut = CreateLegalHoldSut(
            Substitute.For<IReadModelRepository<LegalHoldReadModel>>(),
            recordReadModelRepository,
            retentionRecordService,
            logger,
            repository);

        var result = await sut.PlaceHoldAsync("entity-1", "reason", "applier-1");

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    [Fact]
    public async Task LiftHoldAsync_OtherHoldsQueryReturnsError_LogsOnlyTheErrorCode()
    {
        var holdId = Guid.NewGuid();
        var aggregate = LegalHoldAggregate.Place(
            holdId, "entity-1", "reason", "applier-1", Now);

        var repository = Substitute.For<IAggregateRepository<LegalHoldAggregate>>();
        repository.LoadAsync(holdId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, LegalHoldAggregate>(aggregate)));
        repository.SaveAsync(Arg.Any<LegalHoldAggregate>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Right<EncinaError, Unit>(unit)));

        var readModelRepository = Substitute.For<IReadModelRepository<LegalHoldReadModel>>();
        readModelRepository.QueryAsync(
                Arg.Any<Func<IQueryable<LegalHoldReadModel>, IQueryable<LegalHoldReadModel>>>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Left<EncinaError, IReadOnlyList<LegalHoldReadModel>>(SensitiveError)));

        var logger = new FakeLogger<DefaultLegalHoldService>();
        var sut = CreateLegalHoldSut(
            readModelRepository,
            Substitute.For<IReadModelRepository<RetentionRecordReadModel>>(),
            Substitute.For<IRetentionRecordService>(),
            logger,
            repository);

        var result = await sut.LiftHoldAsync(holdId, "releaser-1");

        result.IsLeft.ShouldBeTrue();
        var logs = logger.Collector.GetSnapshot();
        logs.ShouldContain(r => r.Message.Contains("retention.store_error"));
        logs.ShouldAllBe(r => !r.Message.Contains(SentinelMessage));
    }

    #endregion
}
