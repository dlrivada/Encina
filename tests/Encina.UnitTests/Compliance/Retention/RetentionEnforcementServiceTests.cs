using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Abstractions;
using Encina.Compliance.Retention.Model;
using Encina.Compliance.Retention.ReadModels;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="RetentionEnforcementService"/>.
/// </summary>
/// <remarks>
/// Cycle tests drive <c>ExecuteEnforcementCycleAsync</c> directly so that each test runs exactly
/// the cycles it asserts on, without waiting on the periodic timer.
/// </remarks>
public sealed class RetentionEnforcementServiceTests
{
    private const int CycleCompletedEventId = 8512;
    private const int LegalHoldCheckFailedEventId = 8586;
    private const int TransitionFailedEventId = 8587;
    private const int ErasureIncompleteEventId = 8588;
    private const int ErasureFailedEventId = 8557;

    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly IRetentionRecordService _recordService = Substitute.For<IRetentionRecordService>();
    private readonly ILegalHoldService _legalHoldService = Substitute.For<ILegalHoldService>();
    private readonly IDataErasureExecutor _erasureExecutor = Substitute.For<IDataErasureExecutor>();
    private readonly FakeLogger<RetentionEnforcementService> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    private IServiceScopeFactory CreateScopeFactory(bool withErasureExecutor = true, IEncina? encina = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_recordService);
        services.AddSingleton(_legalHoldService);
        if (withErasureExecutor)
        {
            services.AddSingleton(_erasureExecutor);
        }

        if (encina is not null)
        {
            services.AddSingleton(encina);
        }

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private RetentionEnforcementService CreateSut(
        bool withErasureExecutor = true,
        bool publishNotifications = false,
        IEncina? encina = null) =>
        new(
            CreateScopeFactory(withErasureExecutor, encina),
            Options.Create(new RetentionOptions
            {
                EnableAutomaticEnforcement = true,
                EnforcementInterval = TimeSpan.FromHours(1),
                PublishNotifications = publishNotifications
            }),
            _logger,
            _timeProvider);

    private static RetentionRecordReadModel Record(string entityId, RetentionStatus status = RetentionStatus.Active) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        DataCategory = "test-data",
        ExpiresAtUtc = Now.AddDays(-1),
        Status = status
    };

    private void GivenExpiredRecords(params RetentionRecordReadModel[] records) =>
        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(records));

    private void GivenNoHolds() =>
        _legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false));

    private void GivenTransitionsSucceed()
    {
        _recordService.MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
        _recordService.MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
        _recordService.HoldRecordAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
    }

    private void GivenErasureSucceeds() =>
        _erasureExecutor.EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ErasureResult>(Erasure(fieldsFailed: 0)));

    private static ErasureResult Erasure(int fieldsFailed) => new()
    {
        FieldsErased = 3,
        FieldsRetained = 0,
        FieldsFailed = fieldsFailed,
        RetentionReasons = [],
        Exemptions = []
    };

    private (int Deleted, int Failed, int Held) LastCycleCounts()
    {
        var completed = _logger.Collector.GetSnapshot().Last(r => r.Id.Id == CycleCompletedEventId);
        return (
            int.Parse(completed.GetStructuredStateValue("RecordsDeleted")!, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(completed.GetStructuredStateValue("RecordsFailed")!, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(completed.GetStructuredStateValue("RecordsUnderHold")!, System.Globalization.CultureInfo.InvariantCulture));
    }

    private int LogCount(int eventId) => _logger.Collector.GetSnapshot().Count(r => r.Id.Id == eventId);

    // ========================================================================
    // Hosted-service behaviour
    // ========================================================================

    [Fact]
    public async Task ExecuteAsync_DisabledEnforcement_DoesNotExecuteCycle()
    {
        var options = new RetentionOptions { EnableAutomaticEnforcement = false };
        var sut = new RetentionEnforcementService(
            CreateScopeFactory(),
            Options.Create(options),
            NullLogger<RetentionEnforcementService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await sut.StartAsync(cts.Token);
        await Task.Delay(100, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.DidNotReceive().GetExpiredRecordsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_EnabledEnforcement_ExecutesCycleImmediately()
    {
        GivenExpiredRecords();
        var sut = CreateSut();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await sut.StartAsync(cts.Token);
        await Task.Delay(500, CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);

        await _recordService.Received(1).GetExpiredRecordsAsync(Arg.Any<CancellationToken>());
    }

    // ========================================================================
    // Lifecycle transitions (#1142)
    // ========================================================================

    [Fact]
    public async Task Cycle_ActiveExpiredRecord_MarksExpiredThenErasesThenMarksDeleted()
    {
        var record = Record("entity-1");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

#pragma warning disable CA2012 // NSubstitute call-order specification, the ValueTasks are never awaited
        Received.InOrder(() =>
        {
            _recordService.MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
            _erasureExecutor.EraseAsync("entity-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
            _recordService.MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
        });
#pragma warning restore CA2012
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Cycle_RecordAlreadyExpired_DoesNotMarkExpiredAgain_ErasesAndMarksDeleted()
    {
        var record = Record("entity-retry", RetentionStatus.Expired);
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _erasureExecutor.Received(1).EraseAsync("entity-retry", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Cycle_NoErasureExecutor_MarksExpiredAndDeleted()
    {
        var record = Record("entity-degraded");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();

        await CreateSut(withErasureExecutor: false).ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Cycle_MarkExpiredReturnsLeft_DoesNotErase_CountsAsFailed()
    {
        var record = Record("entity-expire-fails");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _recordService.MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(RetentionErrors.InvalidStateTransition(record.Id, "MarkExpired")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(TransitionFailedEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_ErasureReturnsLeft_DoesNotMarkDeleted_CountsAsFailed()
    {
        var record = Record("entity-erase-fails");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        _erasureExecutor.EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, ErasureResult>(EncinaError.New("erasure store unavailable")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(ErasureFailedEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_ErasureWithFailedFields_DoesNotMarkDeleted_CountsAsFailed()
    {
        var record = Record("entity-partial");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        _erasureExecutor.EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, ErasureResult>(Erasure(fieldsFailed: 2)));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(ErasureIncompleteEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_ErasureThrows_CountsAsFailed_AndContinuesWithNextRecord()
    {
        var failing = Record("entity-throws");
        var healthy = Record("entity-ok");
        GivenExpiredRecords(failing, healthy);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _erasureExecutor.EraseAsync("entity-throws", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, ErasureResult>>>(_ => throw new InvalidOperationException("DB error"));
#pragma warning restore CA2012

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkDeletedAsync(failing.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(healthy.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 1, 0));
    }

    [Fact]
    public async Task Cycle_MarkDeletedReturnsLeft_CountsAsFailed_NotDeleted()
    {
        var record = Record("entity-delete-fails");
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _recordService.MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(RetentionErrors.InvalidStateTransition(record.Id, "MarkDeleted")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(TransitionFailedEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_ErasureFailsThenSucceeds_RetriesOnNextCycleWithoutMarkingExpiredTwice()
    {
        var record = Record("entity-flaky");
        var retried = new RetentionRecordReadModel
        {
            Id = record.Id,
            EntityId = record.EntityId,
            DataCategory = record.DataCategory,
            ExpiresAtUtc = record.ExpiresAtUtc,
            Status = RetentionStatus.Expired
        };
        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(new[] { record }),
                Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(new[] { retried }));
        GivenNoHolds();
        GivenTransitionsSucceed();
        _erasureExecutor.EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(
                Left<EncinaError, ErasureResult>(EncinaError.New("transient failure")),
                Right<EncinaError, ErasureResult>(Erasure(fieldsFailed: 0)));
        var sut = CreateSut();

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        LastCycleCounts().ShouldBe((0, 1, 0));

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        LastCycleCounts().ShouldBe((1, 0, 0));

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _erasureExecutor.Received(2).EraseAsync("entity-flaky", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cycle_SecondCycleAfterDeletion_DoesNotEraseAgain()
    {
        var record = Record("entity-once");
        // After the first cycle the record is Deleted, so the query no longer returns it.
        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(
                Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(new[] { record }),
                Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(System.Array.Empty<RetentionRecordReadModel>()));
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        var sut = CreateSut();

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _erasureExecutor.Received(1).EraseAsync("entity-once", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cycle_GetExpiredReturnsError_DoesNotDelete()
    {
        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                RetentionErrors.StoreError("GetExpired", "Connection failed")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ========================================================================
    // Legal holds (#1143)
    // ========================================================================

    [Fact]
    public async Task Cycle_EntityUnderHold_HoldsRecord_DoesNotErase()
    {
        var record = Record("entity-held");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        _legalHoldService.HasActiveHoldsAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).HoldRecordAsync(record.Id, Guid.Empty, Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 0, 1));
    }

    [Fact]
    public async Task Cycle_HoldLookupReturnsLeft_FailsClosed_SkipsRecordWithoutErasing()
    {
        var record = Record("entity-hold-unknown");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _legalHoldService.HasActiveHoldsAsync("entity-hold-unknown", Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, bool>(EncinaError.New("legal hold store unavailable")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(LegalHoldCheckFailedEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_HoldLookupFailsThenSucceeds_RetriesRecordOnNextCycle()
    {
        var record = Record("entity-hold-retry");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _legalHoldService.HasActiveHoldsAsync("entity-hold-retry", Arg.Any<CancellationToken>())
            .Returns(
                Left<EncinaError, bool>(EncinaError.New("legal hold store unavailable")),
                Right<EncinaError, bool>(false));
        var sut = CreateSut();

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _erasureExecutor.Received(1).EraseAsync("entity-hold-retry", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cycle_HoldRecordReturnsLeft_CountsAsFailed_DoesNotErase()
    {
        var record = Record("entity-hold-transition-fails");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        _legalHoldService.HasActiveHoldsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(true));
        _recordService.HoldRecordAsync(record.Id, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(RetentionErrors.InvalidStateTransition(record.Id, "HoldRecord")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _erasureExecutor.DidNotReceive().EraseAsync(Arg.Any<string>(), Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
    }

    // ========================================================================
    // TimeProvider (#1146)
    // ========================================================================

    [Fact]
    public async Task Cycle_ExpiryAlert_UsesTimeProviderForTheAlertWindow()
    {
        // The alert check runs after the processing of expired records, so give the cycle one.
        GivenExpiredRecords(Record("entity-expired"));
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        var encina = Substitute.For<IEncina>();
        var expiringSoon = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "entity-soon",
            DataCategory = "test-data",
            ExpiresAtUtc = Now.AddDays(3),
            Status = RetentionStatus.Active
        };
        var expiringLater = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "entity-later",
            DataCategory = "test-data",
            ExpiresAtUtc = Now.AddDays(90),
            Status = RetentionStatus.Active
        };
        _recordService.GetRecordsByStatusAsync(RetentionStatus.Active, Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(new[] { expiringSoon, expiringLater }));

        await CreateSut(publishNotifications: true, encina: encina).ExecuteEnforcementCycleAsync(CancellationToken.None);

        await encina.Received(1).Publish(
            Arg.Is<DataExpiringNotification>(n =>
                n.EntityId == "entity-soon" && n.DaysUntilExpiration == 3 && n.OccurredAtUtc == Now),
            Arg.Any<CancellationToken>());
        await encina.DidNotReceive().Publish(
            Arg.Is<DataExpiringNotification>(n => n.EntityId == "entity-later"),
            Arg.Any<CancellationToken>());
    }
}
