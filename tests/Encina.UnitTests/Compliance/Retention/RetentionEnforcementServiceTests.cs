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
    private const int ErasureFailedEventId = 8557;
    private const int DataEraserMissingEventId = 8519;

    private static readonly DateTimeOffset Now = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly IRetentionRecordService _recordService = Substitute.For<IRetentionRecordService>();
    private readonly ILegalHoldService _legalHoldService = Substitute.For<ILegalHoldService>();
    private readonly IRetentionDataEraser _dataEraser = Substitute.For<IRetentionDataEraser>();
    private readonly FakeLogger<RetentionEnforcementService> _logger = new();
    private readonly FakeTimeProvider _timeProvider = new(Now);

    private IServiceScopeFactory CreateScopeFactory(bool withDataEraser = true, IEncina? encina = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_recordService);
        services.AddSingleton(_legalHoldService);
        if (withDataEraser)
        {
            services.AddSingleton(_dataEraser);
        }

        if (encina is not null)
        {
            services.AddSingleton(encina);
        }

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IServiceScopeFactory>();
    }

    private RetentionEnforcementService CreateSut(
        bool withDataEraser = true,
        bool publishNotifications = false,
        IEncina? encina = null) =>
        new(
            CreateScopeFactory(withDataEraser, encina),
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
        _dataEraser.EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));

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
            _dataEraser.EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-1"), Arg.Any<CancellationToken>());
            _recordService.MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
        });
#pragma warning restore CA2012
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    // ========================================================================
    // Category-scoped erasure (#1160)
    // ========================================================================

    [Fact]
    public async Task Cycle_EntityWithTwoCategories_OnlyExpiredCategoryIsErased()
    {
        // The same patient is tracked by two records: contact data (expired) and the clinical
        // record (still within its retention period, so GetExpiredRecordsAsync does not return it).
        var contact = new RetentionRecordReadModel
        {
            Id = Guid.NewGuid(),
            EntityId = "patient-7",
            DataCategory = "patient-contact",
            ExpiresAtUtc = Now.AddDays(-1),
            Status = RetentionStatus.Active
        };
        GivenExpiredRecords(contact);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _dataEraser.Received(1).EraseAsync(
            Arg.Is<RetentionErasureTarget>(t => t.EntityId == "patient-7" && t.DataCategory == "patient-contact"),
            Arg.Any<CancellationToken>());
        await _dataEraser.DidNotReceive().EraseAsync(
            Arg.Is<RetentionErasureTarget>(t => t.DataCategory != "patient-contact"),
            Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(contact.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Cycle_TwoExpiredCategoriesOfOneEntity_EachErasedOnceWithItsOwnCategory()
    {
        var contact = Record("patient-8");
        contact.DataCategory = "patient-contact";
        var billing = Record("patient-8");
        billing.DataCategory = "billing";
        GivenExpiredRecords(contact, billing);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _dataEraser.Received(1).EraseAsync(
            Arg.Is<RetentionErasureTarget>(t => t.RecordId == contact.Id && t.DataCategory == "patient-contact"),
            Arg.Any<CancellationToken>());
        await _dataEraser.Received(1).EraseAsync(
            Arg.Is<RetentionErasureTarget>(t => t.RecordId == billing.Id && t.DataCategory == "billing"),
            Arg.Any<CancellationToken>());
        await _dataEraser.Received(2).EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((2, 0, 0));
    }

    [Fact]
    public async Task Cycle_ErasureTarget_CarriesTheRecordScope()
    {
        var record = Record("entity-scoped");
        record.DataCategory = "clinical-record";
        record.TenantId = "tenant-a";
        record.ModuleId = "module-b";
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        RetentionErasureTarget? captured = null;
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _dataEraser.EraseAsync(Arg.Do<RetentionErasureTarget>(t => captured = t), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(unit));
#pragma warning restore CA2012

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        captured.ShouldNotBeNull();
        captured.ShouldBe(new RetentionErasureTarget
        {
            RecordId = record.Id,
            EntityId = "entity-scoped",
            DataCategory = "clinical-record",
            ExpiresAtUtc = record.ExpiresAtUtc,
            TenantId = "tenant-a",
            ModuleId = "module-b"
        });
    }

    [Fact]
    public async Task Cycle_ErasureReturnsLeft_LogsEntityAndCategory()
    {
        var record = Record("entity-category-log");
        record.DataCategory = "clinical-record";
        GivenExpiredRecords(record);
        GivenNoHolds();
        GivenTransitionsSucceed();
        _dataEraser.EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaError.New("clinical store unavailable")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        var failure = _logger.Collector.GetSnapshot().Single(r => r.Id.Id == ErasureFailedEventId);
        failure.GetStructuredStateValue("EntityId").ShouldBe("entity-category-log");
        failure.GetStructuredStateValue("DataCategory").ShouldBe("clinical-record");
        failure.GetStructuredStateValue("ErrorMessage").ShouldBe("clinical store unavailable");
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
        await _dataEraser.Received(1).EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-retry"), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 0, 0));
    }

    [Fact]
    public async Task Cycle_NoDataEraser_LeavesRecordsExpired_CountsAsFailed_WarnsOncePerCycle()
    {
        var first = Record("entity-no-executor-1");
        var second = Record("entity-no-executor-2");
        GivenExpiredRecords(first, second);
        GivenNoHolds();
        GivenTransitionsSucceed();

        await CreateSut(withDataEraser: false).ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).MarkExpiredAsync(first.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkExpiredAsync(second.Id, Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 2, 0));
        var warnings = _logger.Collector.GetSnapshot().Where(r => r.Id.Id == DataEraserMissingEventId).ToList();
        warnings.Count.ShouldBe(1);
        warnings[0].Level.ShouldBe(Microsoft.Extensions.Logging.LogLevel.Warning);
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

        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
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
        _dataEraser.EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Unit>(EncinaError.New("erasure store unavailable")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(ErasureFailedEventId).ShouldBe(1);
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
        _dataEraser.EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-throws"), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new InvalidOperationException("DB error"));
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
        _dataEraser.EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>())
            .Returns(
                Left<EncinaError, Unit>(EncinaError.New("transient failure")),
                Right<EncinaError, Unit>(unit));
        var sut = CreateSut();

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        LastCycleCounts().ShouldBe((0, 1, 0));

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);
        LastCycleCounts().ShouldBe((1, 0, 0));

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _dataEraser.Received(2).EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-flaky"), Arg.Any<CancellationToken>());
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

        await _dataEraser.Received(1).EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-once"), Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(record.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cycle_GetExpiredReturnsError_DoesNotDelete()
    {
        _recordService.GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, IReadOnlyList<RetentionRecordReadModel>>(
                RetentionErrors.StoreError("GetExpired", "Connection failed")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
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
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
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
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
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
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());

        await sut.ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _dataEraser.Received(1).EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-hold-retry"), Arg.Any<CancellationToken>());
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

        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
    }

    [Fact]
    public async Task Cycle_HoldLookupThrows_FailsClosed_DoesNotErase_CountsAsFailed()
    {
        var record = Record("entity-hold-throws");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _legalHoldService.HasActiveHoldsAsync("entity-hold-throws", Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, bool>>>(_ => throw new InvalidOperationException("legal hold store down"));
#pragma warning restore CA2012

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkExpiredAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(LegalHoldCheckFailedEventId).ShouldBe(1);
    }

    [Fact]
    public async Task Cycle_HoldPlacedAfterFirstCheck_RecheckBeforeErasure_HoldsRecord_DoesNotErase()
    {
        var record = Record("entity-hold-race");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _legalHoldService.HasActiveHoldsAsync("entity-hold-race", Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, bool>(false), Right<EncinaError, bool>(true));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _legalHoldService.Received(2).HasActiveHoldsAsync("entity-hold-race", Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).HoldRecordAsync(record.Id, Guid.Empty, Arg.Any<CancellationToken>());
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 0, 1));
    }

    [Fact]
    public async Task Cycle_HoldRecheckReturnsLeft_FailsClosed_DoesNotErase_CountsAsFailed()
    {
        var record = Record("entity-recheck-unknown");
        GivenExpiredRecords(record);
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        _legalHoldService.HasActiveHoldsAsync("entity-recheck-unknown", Arg.Any<CancellationToken>())
            .Returns(
                Right<EncinaError, bool>(false),
                Left<EncinaError, bool>(EncinaError.New("legal hold store unavailable")));

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.Received(1).MarkExpiredAsync(record.Id, Arg.Any<CancellationToken>());
        await _dataEraser.DidNotReceive().EraseAsync(Arg.Any<RetentionErasureTarget>(), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((0, 1, 0));
        LogCount(LegalHoldCheckFailedEventId).ShouldBe(1);
    }

    // ========================================================================
    // Cancellation
    // ========================================================================

    [Fact]
    public async Task Cycle_ForeignCancellationDuringErasure_CountsRecordAsFailed_AndContinuesWithNextRecord()
    {
        var timedOut = Record("entity-timeout");
        var healthy = Record("entity-after-timeout");
        GivenExpiredRecords(timedOut, healthy);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _dataEraser.EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-timeout"), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ => throw new TaskCanceledException("HTTP request timed out"));
#pragma warning restore CA2012

        await CreateSut().ExecuteEnforcementCycleAsync(CancellationToken.None);

        await _recordService.DidNotReceive().MarkDeletedAsync(timedOut.Id, Arg.Any<CancellationToken>());
        await _recordService.Received(1).MarkDeletedAsync(healthy.Id, Arg.Any<CancellationToken>());
        LastCycleCounts().ShouldBe((1, 1, 0));
    }

    [Fact]
    public async Task Cycle_OwnCancellationDuringErasure_StopsCycle_WithoutProcessingNextRecord()
    {
        var first = Record("entity-cancelled");
        var second = Record("entity-not-reached");
        GivenExpiredRecords(first, second);
        GivenNoHolds();
        GivenTransitionsSucceed();
        GivenErasureSucceeds();
        using var cts = new CancellationTokenSource();
#pragma warning disable CA2012 // NSubstitute mock setup for ValueTask-returning method
        _dataEraser.EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-cancelled"), Arg.Any<CancellationToken>())
            .Returns<ValueTask<Either<EncinaError, Unit>>>(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });
#pragma warning restore CA2012

        await CreateSut().ExecuteEnforcementCycleAsync(cts.Token);

        await _dataEraser.DidNotReceive().EraseAsync(Arg.Is<RetentionErasureTarget>(t => t.EntityId == "entity-not-reached"), Arg.Any<CancellationToken>());
        await _recordService.DidNotReceive().MarkDeletedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        LogCount(CycleCompletedEventId).ShouldBe(0);
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
