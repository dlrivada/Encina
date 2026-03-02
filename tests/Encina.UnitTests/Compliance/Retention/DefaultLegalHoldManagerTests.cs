#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.Retention;
using Encina.Compliance.Retention.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.Retention;

/// <summary>
/// Unit tests for <see cref="DefaultLegalHoldManager"/>.
/// </summary>
public class DefaultLegalHoldManagerTests
{
    private readonly ILegalHoldStore _holdStore = Substitute.For<ILegalHoldStore>();
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly IRetentionAuditStore _auditStore = Substitute.For<IRetentionAuditStore>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly DefaultLegalHoldManager _sut;

    public DefaultLegalHoldManagerTests()
    {
        // Audit store always succeeds by default
        _auditStore
            .RecordAsync(Arg.Any<RetentionAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        _sut = new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullHoldStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultLegalHoldManager(
            null!,
            _recordStore,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("holdStore");
    }

    [Fact]
    public void Constructor_NullRecordStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultLegalHoldManager(
            _holdStore,
            null!,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("recordStore");
    }

    [Fact]
    public void Constructor_NullAuditStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            null!,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("auditStore");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            null!,
            NullLogger<DefaultLegalHoldManager>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            _timeProvider,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullEncinaIsAllowed()
    {
        // Act — encina is optional (default null)
        var act = () => new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance,
            encina: null);

        // Assert
        act.Should().NotThrow();
    }

    #endregion

    #region ApplyHoldAsync Tests

    [Fact]
    public async Task ApplyHoldAsync_ValidHold_CreatesHoldAndUpdatesRecords()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Pending tax audit", "legal@company.com");
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create("entity-1", "financial-records", now.AddDays(-10), now.AddDays(355));

        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _holdStore
            .CreateAsync(hold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.ApplyHoldAsync("entity-1", hold);

        // Assert
        result.IsRight.Should().BeTrue();
        await _holdStore.Received(1).CreateAsync(hold, Arg.Any<CancellationToken>());
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.UnderLegalHold, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyHoldAsync_AlreadyUnderHold_ReturnsLeftError()
    {
        // Arrange
        var hold = LegalHold.Create("entity-held", "Litigation hold");
        _holdStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

        // Act
        var result = await _sut.ApplyHoldAsync("entity-held", hold);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyActiveCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ApplyHoldAsync_AlreadyUnderHold_DoesNotCallCreateAsync()
    {
        // Arrange
        var hold = LegalHold.Create("entity-held", "Litigation hold");
        _holdStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

        // Act
        await _sut.ApplyHoldAsync("entity-held", hold);

        // Assert
        await _holdStore.DidNotReceive().CreateAsync(Arg.Any<LegalHold>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyHoldAsync_WithEncina_PublishesNotification()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina
            .Publish(Arg.Any<LegalHoldAppliedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sutWithEncina = new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance,
            encina);

        var hold = LegalHold.Create("entity-1", "Audit investigation");
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _holdStore
            .CreateAsync(hold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        // Act
        var result = await sutWithEncina.ApplyHoldAsync("entity-1", hold);

        // Assert
        result.IsRight.Should().BeTrue();
        await encina.Received(1).Publish(
            Arg.Any<LegalHoldAppliedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyHoldAsync_StoreCreateFails_ReturnsLeftError()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Litigation hold");
        var storeError = RetentionErrors.StoreError("CreateAsync", "insert failed");

        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _holdStore
            .CreateAsync(hold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, Unit>(storeError)));

        // Act
        var result = await _sut.ApplyHoldAsync("entity-1", hold);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ApplyHoldAsync_RecordsAuditEntry()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Compliance audit");
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _holdStore
            .CreateAsync(hold, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        // Act
        await _sut.ApplyHoldAsync("entity-1", hold);

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<RetentionAuditEntry>(e => e.Action == "LegalHoldApplied"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ApplyHoldAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Audit");

        // Act
        var act = async () => await _sut.ApplyHoldAsync(entityId!, hold);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ApplyHoldAsync_NullHold_ShouldThrow()
    {
        // Act
        var act = async () => await _sut.ApplyHoldAsync("entity-1", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region ReleaseHoldAsync Tests

    [Fact]
    public async Task ReleaseHoldAsync_ValidRelease_ReleasesAndUpdatesRecords()
    {
        // Arrange
        var now = _timeProvider.GetUtcNow();
        var hold = LegalHold.Create("entity-1", "Tax audit completed", "legal@company.com");
        var record = RetentionRecord.Create(
            "entity-1", "financial-records",
            now.AddDays(-10), now.AddDays(355)) with
        { Status = RetentionStatus.UnderLegalHold };

        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, "admin", now, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _recordStore
            .UpdateStatusAsync(record.Id, Arg.Any<RetentionStatus>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.ReleaseHoldAsync(hold.Id, "admin");

        // Assert
        result.IsRight.Should().BeTrue();
        await _holdStore.Received(1).ReleaseAsync(
            hold.Id,
            "admin",
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_HoldNotFound_ReturnsLeftError()
    {
        // Arrange
        _holdStore
            .GetByIdAsync("nonexistent-hold", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Option<LegalHold>.None)));

        // Act
        var result = await _sut.ReleaseHoldAsync("nonexistent-hold", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ReleaseHoldAsync_HoldAlreadyReleased_ReturnsLeftError()
    {
        // Arrange — hold was already released (ReleasedAtUtc is set)
        var releasedHold = new LegalHold
        {
            Id = "hold-1",
            EntityId = "entity-1",
            Reason = "Resolved",
            AppliedAtUtc = _timeProvider.GetUtcNow().AddDays(-30),
            ReleasedAtUtc = _timeProvider.GetUtcNow().AddDays(-5),
            ReleasedByUserId = "admin"
        };

        _holdStore
            .GetByIdAsync("hold-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(releasedHold))));

        // Act
        var result = await _sut.ReleaseHoldAsync("hold-1", "admin");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.HoldAlreadyReleasedCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task ReleaseHoldAsync_NoOtherActiveHolds_ResetsStatusToActive()
    {
        // Arrange — record has not expired (expires in future), so should become Active
        var now = _timeProvider.GetUtcNow();
        var hold = LegalHold.Create("entity-1", "Audit completed");
        var record = RetentionRecord.Create(
            "entity-1", "personal-data",
            now.AddDays(-10), now.AddDays(355)) with
        { Status = RetentionStatus.UnderLegalHold };

        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, null, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false))); // no other active holds
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Active, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.ReleaseHoldAsync(hold.Id, null);

        // Assert
        result.IsRight.Should().BeTrue();
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.Active, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_NoOtherActiveHolds_ExpiredRecord_ResetsStatusToExpired()
    {
        // Arrange — record is past expiry, so should become Expired
        var now = _timeProvider.GetUtcNow();
        var hold = LegalHold.Create("entity-1", "Audit completed");
        var record = RetentionRecord.Create(
            "entity-1", "personal-data",
            now.AddDays(-400), now.AddDays(-35)) with // expired 35 days ago
        { Status = RetentionStatus.UnderLegalHold };

        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, null, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));
        _recordStore
            .UpdateStatusAsync(record.Id, RetentionStatus.Expired, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        // Act
        var result = await _sut.ReleaseHoldAsync(hold.Id, null);

        // Assert
        result.IsRight.Should().BeTrue();
        await _recordStore.Received(1).UpdateStatusAsync(
            record.Id, RetentionStatus.Expired, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_OtherActiveHoldsRemain_DoesNotUpdateRecordStatus()
    {
        // Arrange — another hold is still active, so record status must not change
        var now = _timeProvider.GetUtcNow();
        var hold = LegalHold.Create("entity-1", "First hold");
        var record = RetentionRecord.Create(
            "entity-1", "personal-data",
            now.AddDays(-10), now.AddDays(355)) with
        { Status = RetentionStatus.UnderLegalHold };

        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, null, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true))); // another hold remains
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        // Act
        var result = await _sut.ReleaseHoldAsync(hold.Id, null);

        // Assert
        result.IsRight.Should().BeTrue();
        await _recordStore.DidNotReceive().UpdateStatusAsync(
            Arg.Any<string>(), Arg.Any<RetentionStatus>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_WithEncina_PublishesNotification()
    {
        // Arrange
        var encina = Substitute.For<IEncina>();
        encina
            .Publish(Arg.Any<LegalHoldReleasedNotification>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

        var sutWithEncina = new DefaultLegalHoldManager(
            _holdStore,
            _recordStore,
            _auditStore,
            _timeProvider,
            NullLogger<DefaultLegalHoldManager>.Instance,
            encina);

        var hold = LegalHold.Create("entity-1", "Audit done");
        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, "admin", Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        // Act
        var result = await sutWithEncina.ReleaseHoldAsync(hold.Id, "admin");

        // Assert
        result.IsRight.Should().BeTrue();
        await encina.Received(1).Publish(
            Arg.Any<LegalHoldReleasedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseHoldAsync_RecordsAuditEntry()
    {
        // Arrange
        var hold = LegalHold.Create("entity-1", "Audit closed");
        _holdStore
            .GetByIdAsync(hold.Id, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<LegalHold>>(Some(hold))));
        _holdStore
            .ReleaseAsync(hold.Id, null, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        // Act
        await _sut.ReleaseHoldAsync(hold.Id, null);

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<RetentionAuditEntry>(e => e.Action == "LegalHoldReleased"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ReleaseHoldAsync_InvalidHoldId_ShouldThrow(string? holdId)
    {
        // Act
        var act = async () => await _sut.ReleaseHoldAsync(holdId!, "admin");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsUnderHoldAsync Tests

    [Fact]
    public async Task IsUnderHoldAsync_DelegatesToStore_WhenUnderHold()
    {
        // Arrange
        _holdStore
            .IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(true)));

        // Act
        var result = await _sut.IsUnderHoldAsync("entity-held");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = result.RightAsEnumerable().First();
        isHeld.Should().BeTrue();
        await _holdStore.Received(1).IsUnderHoldAsync("entity-held", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsUnderHoldAsync_DelegatesToStore_WhenNotUnderHold()
    {
        // Arrange
        _holdStore
            .IsUnderHoldAsync("entity-free", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(false)));

        // Act
        var result = await _sut.IsUnderHoldAsync("entity-free");

        // Assert
        result.IsRight.Should().BeTrue();
        var isHeld = result.RightAsEnumerable().First();
        isHeld.Should().BeFalse();
    }

    [Fact]
    public async Task IsUnderHoldAsync_StoreReturnsError_PropagatesError()
    {
        // Arrange
        var storeError = RetentionErrors.StoreError("IsUnderHoldAsync", "connection failed");
        _holdStore
            .IsUnderHoldAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, bool>(storeError)));

        // Act
        var result = await _sut.IsUnderHoldAsync("entity-1");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsUnderHoldAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = async () => await _sut.IsUnderHoldAsync(entityId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetActiveHoldsAsync Tests

    [Fact]
    public async Task GetActiveHoldsAsync_DelegatesToStore()
    {
        // Arrange
        var hold1 = LegalHold.Create("entity-1", "First hold");
        var hold2 = LegalHold.Create("entity-2", "Second hold");
        IReadOnlyList<LegalHold> holds = [hold1, hold2];

        _holdStore
            .GetActiveHoldsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<LegalHold>>(holds)));

        // Act
        var result = await _sut.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var returnedHolds = result.RightAsEnumerable().First();
        returnedHolds.Should().HaveCount(2);
        await _holdStore.Received(1).GetActiveHoldsAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetActiveHoldsAsync_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        IReadOnlyList<LegalHold> emptyHolds = [];
        _holdStore
            .GetActiveHoldsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, IReadOnlyList<LegalHold>>(emptyHolds)));

        // Act
        var result = await _sut.GetActiveHoldsAsync();

        // Assert
        result.IsRight.Should().BeTrue();
        var returnedHolds = result.RightAsEnumerable().First();
        returnedHolds.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveHoldsAsync_StoreReturnsError_PropagatesError()
    {
        // Arrange
        var storeError = RetentionErrors.StoreError("GetActiveHoldsAsync", "query failed");
        _holdStore
            .GetActiveHoldsAsync(Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<LegalHold>>(storeError)));

        // Act
        var result = await _sut.GetActiveHoldsAsync();

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    #endregion
}
