#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DefaultRetentionEnforcer"/>.
/// </summary>
public class DefaultRetentionEnforcerTests
{
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly ILegalHoldStore _legalHoldStore = Substitute.For<ILegalHoldStore>();
    private readonly IRetentionAuditStore _auditStore = Substitute.For<IRetentionAuditStore>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

    private DefaultRetentionEnforcer CreateSut(
        RetentionOptions? options = null,
        IServiceProvider? serviceProvider = null)
    {
        var opts = Options.Create(options ?? new RetentionOptions
        {
            TrackAuditTrail = true,
            PublishNotifications = true
        });

        // By default no optional services registered
        var sp = serviceProvider ?? _serviceProvider;

        return new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            _auditStore,
            opts,
            sp,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);
    }

    public DefaultRetentionEnforcerTests()
    {
        // Audit store always succeeds by default
        _auditStore
            .RecordAsync(Arg.Any<RetentionAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Service provider returns null for optional services by default
        _serviceProvider.GetService(typeof(IDataErasureExecutor)).Returns(null);
        _serviceProvider.GetService(typeof(IEncina)).Returns(null);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRecordStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            null!,
            _legalHoldStore,
            _auditStore,
            Options.Create(new RetentionOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("recordStore");
    }

    [Fact]
    public void Constructor_NullLegalHoldStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            null!,
            _auditStore,
            Options.Create(new RetentionOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("legalHoldStore");
    }

    [Fact]
    public void Constructor_NullAuditStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            null!,
            Options.Create(new RetentionOptions()),
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            _auditStore,
            null!,
            _serviceProvider,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            _auditStore,
            Options.Create(new RetentionOptions()),
            null!,
            _timeProvider,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("serviceProvider");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            _auditStore,
            Options.Create(new RetentionOptions()),
            _serviceProvider,
            null!,
            NullLogger<DefaultRetentionEnforcer>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionEnforcer(
            _recordStore,
            _legalHoldStore,
            _auditStore,
            Options.Create(new RetentionOptions()),
            _serviceProvider,
            _timeProvider,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidParameters_ShouldNotThrow()
    {
        // Act
        var act = () => CreateSut();

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region EnforceRetentionAsync Tests

    [Fact]
    public async Task EnforceRetentionAsync_NoExpiredRecords_ReturnsEmptyResult()
    {
        // Arrange
        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.TotalRecordsEvaluated.Should().Be(0);
        deletionResult.RecordsDeleted.Should().Be(0);
        deletionResult.RecordsFailed.Should().Be(0);
        deletionResult.RecordsUnderHold.Should().Be(0);
        deletionResult.Details.Should().BeEmpty();
    }

    [Fact]
    public async Task EnforceRetentionAsync_OneExpiredRecord_NotUnderHold_DeletesRecord()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            "entity-1",
            "financial-records",
            now.AddDays(-400),
            now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.TotalRecordsEvaluated.Should().Be(1);
        deletionResult.RecordsDeleted.Should().Be(1);
        deletionResult.RecordsFailed.Should().Be(0);
        deletionResult.RecordsUnderHold.Should().Be(0);
        deletionResult.Details.Should().HaveCount(1);
        deletionResult.Details[0].Outcome.Should().Be(DeletionOutcome.Deleted);
        deletionResult.Details[0].EntityId.Should().Be("entity-1");
    }

    [Fact]
    public async Task EnforceRetentionAsync_OneExpiredRecord_NotUnderHold_UpdatesStatusToDeleted()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            "entity-1",
            "financial-records",
            now.AddDays(-400),
            now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_RecordUnderLegalHold_SkipsDeletion()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            "entity-held",
            "personal-data",
            now.AddDays(-400),
            now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.TotalRecordsEvaluated.Should().Be(1);
        deletionResult.RecordsDeleted.Should().Be(0);
        deletionResult.RecordsUnderHold.Should().Be(1);
        deletionResult.Details[0].Outcome.Should().Be(DeletionOutcome.HeldByLegalHold);
    }

    [Fact]
    public async Task EnforceRetentionAsync_RecordUnderLegalHold_UpdatesStatusToUnderLegalHold()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            "entity-held",
            "personal-data",
            now.AddDays(-400),
            now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>());
        await _recordStore.DidNotReceive().UpdateStatusAsync(
            record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_MultipleRecordsMixed_ReturnsCorrectCounts()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record1 = RetentionRecord.Create("entity-1", "cat-a", now.AddDays(-400), now.AddDays(-10));
        var record2 = RetentionRecord.Create("entity-2", "cat-b", now.AddDays(-400), now.AddDays(-20));
        var record3 = RetentionRecord.Create("entity-3", "cat-c", now.AddDays(-400), now.AddDays(-30));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record1, record2, record3 })));

        // entity-1: under hold
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));
        _recordStore
            .UpdateStatusAsync(record1.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // entity-2: deleted
        _legalHoldStore
            .IsUnderHoldAsync("entity-2", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record2.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // entity-3: deleted
        _legalHoldStore
            .IsUnderHoldAsync("entity-3", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record3.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.TotalRecordsEvaluated.Should().Be(3);
        deletionResult.RecordsDeleted.Should().Be(2);
        deletionResult.RecordsUnderHold.Should().Be(1);
        deletionResult.RecordsFailed.Should().Be(0);
        deletionResult.Details.Should().HaveCount(3);
    }

    [Fact]
    public async Task EnforceRetentionAsync_RecordStoreReturnsError_PropagatesError()
    {
        // Arrange
        var storeError = RetentionErrors.StoreError("GetExpiredRecordsAsync", "connection failed");
        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<RetentionRecord>>(storeError)));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.EnforcementFailedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithErasureExecutor_CallsEraseAsync()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var erasureExecutor = Substitute.For<IDataErasureExecutor>();
        var successfulResult = new ErasureResult
        {
            FieldsErased = 5,
            FieldsRetained = 0,
            FieldsFailed = 0,
            RetentionReasons = [],
            Exemptions = []
        };
        erasureExecutor
            .EraseAsync("entity-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, ErasureResult>(successfulResult)));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(erasureExecutor);
        sp.GetService(typeof(IEncina)).Returns(null);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut(serviceProvider: sp);

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        await erasureExecutor.Received(1).EraseAsync(
            "entity-1",
            Arg.Any<ErasureScope>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithErasureExecutor_ErasureFails_ReturnsFailedOutcome()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var erasureExecutor = Substitute.For<IDataErasureExecutor>();
        var erasureError = EncinaErrors.Create("erasure.failed", "Erasure executor failed");
        erasureExecutor
            .EraseAsync("entity-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, ErasureResult>(erasureError)));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(erasureExecutor);
        sp.GetService(typeof(IEncina)).Returns(null);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));

        var sut = CreateSut(serviceProvider: sp);

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.RecordsFailed.Should().Be(1);
        deletionResult.RecordsDeleted.Should().Be(0);
        deletionResult.Details[0].Outcome.Should().Be(DeletionOutcome.Failed);
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithoutErasureExecutor_DegradedMode_StillMarksAsDeleted()
    {
        // Arrange — no IDataErasureExecutor registered
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // _serviceProvider already returns null for IDataErasureExecutor
        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.RecordsDeleted.Should().Be(1);
        deletionResult.Details[0].Outcome.Should().Be(DeletionOutcome.Deleted);
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithIEncina_PublishesRetentionEnforcementCompletedNotification()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var encina = Substitute.For<IEncina>();
        encina
            .Publish(Arg.Any<RetentionEnforcementCompletedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        encina
            .Publish(Arg.Any<DataDeletedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(null);
        sp.GetService(typeof(IEncina)).Returns(encina);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut(serviceProvider: sp);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await encina.Received(1).Publish(
            Arg.Any<RetentionEnforcementCompletedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithIEncina_PublishesDataDeletedNotificationPerRecord()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var encina = Substitute.For<IEncina>();
        encina
            .Publish(Arg.Any<DataDeletedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        encina
            .Publish(Arg.Any<RetentionEnforcementCompletedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(null);
        sp.GetService(typeof(IEncina)).Returns(encina);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut(serviceProvider: sp);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await encina.Received(1).Publish(
            Arg.Is<DataDeletedNotification>(n => n.EntityId == "entity-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithoutIEncina_DoesNotPublishNotifications()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        // _serviceProvider returns null for IEncina by default
        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sut = CreateSut();

        // Act — should not throw even without IEncina
        var result = await sut.EnforceRetentionAsync();

        // Assert — succeeded without publishing
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.RecordsDeleted.Should().Be(1);
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithTrackAuditTrail_RecordsAuditEntryPerDeletion()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var options = new RetentionOptions { TrackAuditTrail = true, PublishNotifications = false };
        var sut = CreateSut(options);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert — at least one audit entry recorded with "RecordDeleted" action
        await _auditStore.Received().RecordAsync(
            Arg.Is<RetentionAuditEntry>(e => e.Action == "RecordDeleted"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithTrackAuditTrail_RecordsEnforcementExecutedAuditEntry()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var options = new RetentionOptions { TrackAuditTrail = true, PublishNotifications = false };
        var sut = CreateSut(options);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert — enforcement summary audit entry
        await _auditStore.Received().RecordAsync(
            Arg.Is<RetentionAuditEntry>(e => e.Action == "EnforcementExecuted"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithTrackAuditTrail_RecordsLegalHoldAuditEntry()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-held", "personal-data", now.AddDays(-400), now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var options = new RetentionOptions { TrackAuditTrail = true, PublishNotifications = false };
        var sut = CreateSut(options);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert — legal hold skip audit entry
        await _auditStore.Received().RecordAsync(
            Arg.Is<RetentionAuditEntry>(e => e.Action == "DeletionSkippedLegalHold"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithoutTrackAuditTrail_DoesNotRecordAuditEntries()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var options = new RetentionOptions { TrackAuditTrail = false, PublishNotifications = false };
        var sut = CreateSut(options);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await _auditStore.DidNotReceive().RecordAsync(
            Arg.Any<RetentionAuditEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_WithPublishNotificationsDisabled_DoesNotPublish()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var encina = Substitute.For<IEncina>();
        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(null);
        sp.GetService(typeof(IEncina)).Returns(encina);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Deleted, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var options = new RetentionOptions { TrackAuditTrail = false, PublishNotifications = false };
        var sut = CreateSut(options, sp);

        // Act
        await sut.EnforceRetentionAsync();

        // Assert
        await encina.DidNotReceiveWithAnyArgs().Publish(
            Arg.Any<RetentionEnforcementCompletedNotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnforceRetentionAsync_ErasureExecutorThrowsException_ReturnsFailedOutcome()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "personal-data", now.AddDays(-400), now.AddDays(-35));

        var erasureExecutor = Substitute.For<IDataErasureExecutor>();
        erasureExecutor
            .EraseAsync("entity-1", Arg.Any<ErasureScope>(), Arg.Any<CancellationToken>())
            .Returns(_ => ValueTask.FromException<Either<EncinaError, ErasureResult>>(
                new InvalidOperationException("Unexpected error in erasure")));

        var sp = Substitute.For<IServiceProvider>();
        sp.GetService(typeof(IDataErasureExecutor)).Returns(erasureExecutor);
        sp.GetService(typeof(IEncina)).Returns(null);

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _legalHoldStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));

        var sut = CreateSut(serviceProvider: sp);

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert — exception is caught and turned into Failed outcome
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.RecordsFailed.Should().Be(1);
        deletionResult.Details[0].Outcome.Should().Be(DeletionOutcome.Failed);
    }

    [Fact]
    public async Task EnforceRetentionAsync_ExecutedAtUtc_MatchesTimeProvider()
    {
        // Arrange
        var expectedTime = _timeProvider.GetUtcNow();

        _recordStore
            .GetExpiredRecordsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        var sut = CreateSut();

        // Act
        var result = await sut.EnforceRetentionAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var deletionResult = result.RightAsEnumerable().First();
        deletionResult.ExecutedAtUtc.Should().Be(expectedTime);
    }

    #endregion

    #region GetExpiringDataAsync Tests

    [Fact]
    public async Task GetExpiringDataAsync_ReturnsExpiringDataWithCorrectDaysUntilExpiration()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var within = TimeSpan.FromDays(30);

        // Record expires in 7 days
        var record = RetentionRecord.Create(
            "entity-1",
            "personal-data",
            now.AddDays(-358),
            now.AddDays(7));

        _recordStore
            .GetExpiringWithinAsync(within, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExpiringDataAsync(within);

        // Assert
        result.IsRight.Should().BeTrue();
        var expiringList = result.RightAsEnumerable().First();
        expiringList.Should().HaveCount(1);
        expiringList[0].EntityId.Should().Be("entity-1");
        expiringList[0].DataCategory.Should().Be("personal-data");
        expiringList[0].DaysUntilExpiration.Should().Be(7);
        expiringList[0].ExpiresAtUtc.Should().Be(record.ExpiresAtUtc);
    }

    [Fact]
    public async Task GetExpiringDataAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        var within = TimeSpan.FromDays(30);

        _recordStore
            .GetExpiringWithinAsync(within, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExpiringDataAsync(within);

        // Assert
        result.IsRight.Should().BeTrue();
        var expiringList = result.RightAsEnumerable().First();
        expiringList.Should().BeEmpty();
    }

    [Fact]
    public async Task GetExpiringDataAsync_StoreError_PropagatesError()
    {
        // Arrange
        var within = TimeSpan.FromDays(30);
        var storeError = RetentionErrors.StoreError("GetExpiringWithinAsync", "query failed");

        _recordStore
            .GetExpiringWithinAsync(within, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<RetentionRecord>>(storeError)));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExpiringDataAsync(within);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task GetExpiringDataAsync_MultipleRecords_ReturnsMappedExpiringDataList()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var within = TimeSpan.FromDays(30);

        var record1 = RetentionRecord.Create("entity-1", "cat-a", now.AddDays(-365), now.AddDays(5));
        var record2 = RetentionRecord.Create("entity-2", "cat-b", now.AddDays(-355), now.AddDays(15));

        _recordStore
            .GetExpiringWithinAsync(within, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record1, record2 })));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExpiringDataAsync(within);

        // Assert
        result.IsRight.Should().BeTrue();
        var expiringList = result.RightAsEnumerable().First();
        expiringList.Should().HaveCount(2);
        expiringList[0].DaysUntilExpiration.Should().Be(5);
        expiringList[1].DaysUntilExpiration.Should().Be(15);
    }

    [Fact]
    public async Task GetExpiringDataAsync_MapsRecordPolicyId()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var within = TimeSpan.FromDays(30);

        var record = RetentionRecord.Create(
            "entity-1",
            "personal-data",
            now.AddDays(-358),
            now.AddDays(7),
            policyId: "policy-abc");

        _recordStore
            .GetExpiringWithinAsync(within, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        var sut = CreateSut();

        // Act
        var result = await sut.GetExpiringDataAsync(within);

        // Assert
        result.IsRight.Should().BeTrue();
        var expiringList = result.RightAsEnumerable().First();
        expiringList[0].PolicyId.Should().Be("policy-abc");
    }

    #endregion
}
