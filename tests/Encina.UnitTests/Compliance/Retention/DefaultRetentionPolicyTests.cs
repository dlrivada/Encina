#pragma warning disable CA2012 // Use ValueTasks correctly

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
/// Unit tests for <see cref="DefaultRetentionPolicy"/>.
/// </summary>
public class DefaultRetentionPolicyTests
{
    private readonly IRetentionPolicyStore _policyStore = Substitute.For<IRetentionPolicyStore>();
    private readonly IRetentionRecordStore _recordStore = Substitute.For<IRetentionRecordStore>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly RetentionOptions _options = new();
    private readonly DefaultRetentionPolicy _sut;

    public DefaultRetentionPolicyTests()
    {
        _sut = new DefaultRetentionPolicy(
            _policyStore,
            _recordStore,
            _timeProvider,
            Options.Create(_options),
            NullLogger<DefaultRetentionPolicy>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullPolicyStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionPolicy(
            null!,
            _recordStore,
            _timeProvider,
            Options.Create(_options),
            NullLogger<DefaultRetentionPolicy>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("policyStore");
    }

    [Fact]
    public void Constructor_NullRecordStore_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionPolicy(
            _policyStore,
            null!,
            _timeProvider,
            Options.Create(_options),
            NullLogger<DefaultRetentionPolicy>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("recordStore");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionPolicy(
            _policyStore,
            _recordStore,
            null!,
            Options.Create(_options),
            NullLogger<DefaultRetentionPolicy>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("timeProvider");
    }

    [Fact]
    public void Constructor_NullOptions_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionPolicy(
            _policyStore,
            _recordStore,
            _timeProvider,
            null!,
            NullLogger<DefaultRetentionPolicy>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new DefaultRetentionPolicy(
            _policyStore,
            _recordStore,
            _timeProvider,
            Options.Create(_options),
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region GetRetentionPeriodAsync Tests

    [Fact]
    public async Task GetRetentionPeriodAsync_PolicyExists_ReturnsPolicyPeriod()
    {
        // Arrange
        var period = TimeSpan.FromDays(365);
        var policy = RetentionPolicy.Create("personal-data", period);
        _policyStore
            .GetByCategoryAsync("personal-data", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<RetentionPolicy>>(Some(policy))));

        // Act
        var result = await _sut.GetRetentionPeriodAsync("personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var returnedPeriod = result.RightAsEnumerable().First();
        returnedPeriod.Should().Be(period);
    }

    [Fact]
    public async Task GetRetentionPeriodAsync_NoPolicyDefaultExists_ReturnsDefaultPeriod()
    {
        // Arrange
        var defaultPeriod = TimeSpan.FromDays(180);
        _options.DefaultRetentionPeriod = defaultPeriod;
        _policyStore
            .GetByCategoryAsync("unknown-category", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<RetentionPolicy>>(Option<RetentionPolicy>.None)));

        // Act
        var result = await _sut.GetRetentionPeriodAsync("unknown-category");

        // Assert
        result.IsRight.Should().BeTrue();
        var returnedPeriod = result.RightAsEnumerable().First();
        returnedPeriod.Should().Be(defaultPeriod);
    }

    [Fact]
    public async Task GetRetentionPeriodAsync_NoPolicyNoDefault_ReturnsLeftError()
    {
        // Arrange — no default configured, _options.DefaultRetentionPeriod is null by default
        _policyStore
            .GetByCategoryAsync("unknown-category", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, Option<RetentionPolicy>>(Option<RetentionPolicy>.None)));

        // Act
        var result = await _sut.GetRetentionPeriodAsync("unknown-category");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.NoPolicyForCategoryCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task GetRetentionPeriodAsync_StoreReturnsError_PropagatesError()
    {
        // Arrange
        var storeError = RetentionErrors.StoreError("GetByCategoryAsync", "database unavailable");
        _policyStore
            .GetByCategoryAsync("personal-data", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, Option<RetentionPolicy>>(storeError)));

        // Act
        var result = await _sut.GetRetentionPeriodAsync("personal-data");

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
    public async Task GetRetentionPeriodAsync_InvalidDataCategory_ShouldThrow(string? dataCategory)
    {
        // Act
        var act = async () => await _sut.GetRetentionPeriodAsync(dataCategory!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region IsExpiredAsync Tests

    [Fact]
    public async Task IsExpiredAsync_RecordExpired_ReturnsTrue()
    {
        // Arrange
        // Time is frozen at 2026-03-01 12:00:00 UTC; record expired yesterday
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            entityId: "entity-1",
            dataCategory: "personal-data",
            createdAtUtc: now.AddDays(-366),
            expiresAtUtc: now.AddDays(-1));

        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        // Act
        var result = await _sut.IsExpiredAsync("entity-1", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var isExpired = result.RightAsEnumerable().First();
        isExpired.Should().BeTrue();
    }

    [Fact]
    public async Task IsExpiredAsync_RecordNotExpired_ReturnsFalse()
    {
        // Arrange
        // Time is frozen at 2026-03-01 12:00:00 UTC; record expires tomorrow
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            entityId: "entity-1",
            dataCategory: "personal-data",
            createdAtUtc: now.AddDays(-10),
            expiresAtUtc: now.AddDays(1));

        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        // Act
        var result = await _sut.IsExpiredAsync("entity-1", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var isExpired = result.RightAsEnumerable().First();
        isExpired.Should().BeFalse();
    }

    [Fact]
    public async Task IsExpiredAsync_NoRecordFound_ReturnsLeftError()
    {
        // Arrange — store returns empty list (no records for this entity/category)
        _recordStore
            .GetByEntityIdAsync("entity-missing", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord>())));

        // Act
        var result = await _sut.IsExpiredAsync("entity-missing", "personal-data");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task IsExpiredAsync_RecordExistsForDifferentCategory_ReturnsLeftError()
    {
        // Arrange — entity has a record, but for a different data category
        var now = _timeProvider.GetUtcNow();
        var record = RetentionRecord.Create(
            entityId: "entity-1",
            dataCategory: "financial-records",
            createdAtUtc: now.AddDays(-10),
            expiresAtUtc: now.AddDays(355));

        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { record })));

        // Act
        var result = await _sut.IsExpiredAsync("entity-1", "personal-data");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.RecordNotFoundCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task IsExpiredAsync_StoreReturnsError_PropagatesError()
    {
        // Arrange
        var storeError = RetentionErrors.StoreError("GetByEntityIdAsync", "connection timeout");
        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Left<EncinaError, IReadOnlyList<RetentionRecord>>(storeError)));

        // Act
        var result = await _sut.IsExpiredAsync("entity-1", "personal-data");

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = result.LeftAsEnumerable().First();
        error.GetCode().Match(
            Some: code => code.Should().Be(RetentionErrors.StoreErrorCode),
            None: () => Assert.Fail("Expected error code"));
    }

    [Fact]
    public async Task IsExpiredAsync_MultipleRecords_MatchesCorrectCategory()
    {
        // Arrange — entity has two records for different categories; personal-data is not expired
        var now = _timeProvider.GetUtcNow();
        var financialRecord = RetentionRecord.Create(
            entityId: "entity-1",
            dataCategory: "financial-records",
            createdAtUtc: now.AddDays(-3000),
            expiresAtUtc: now.AddDays(-1)); // expired

        var personalRecord = RetentionRecord.Create(
            entityId: "entity-1",
            dataCategory: "personal-data",
            createdAtUtc: now.AddDays(-10),
            expiresAtUtc: now.AddDays(355)); // not expired

        _recordStore
            .GetByEntityIdAsync("entity-1", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(
                Right<EncinaError, IReadOnlyList<RetentionRecord>>(
                    new List<RetentionRecord> { financialRecord, personalRecord })));

        // Act
        var result = await _sut.IsExpiredAsync("entity-1", "personal-data");

        // Assert
        result.IsRight.Should().BeTrue();
        var isExpired = result.RightAsEnumerable().First();
        isExpired.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsExpiredAsync_InvalidEntityId_ShouldThrow(string? entityId)
    {
        // Act
        var act = async () => await _sut.IsExpiredAsync(entityId!, "personal-data");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsExpiredAsync_InvalidDataCategory_ShouldThrow(string? dataCategory)
    {
        // Act
        var act = async () => await _sut.IsExpiredAsync("entity-1", dataCategory!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
