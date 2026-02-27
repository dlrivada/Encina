using Encina.Compliance.DataSubjectRights;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.Logging;
using NSubstitute;

using static LanguageExt.Prelude;

#pragma warning disable CA2012 // Use ValueTasks correctly

namespace Encina.UnitTests.Compliance.DataSubjectRights;

/// <summary>
/// Unit tests for <see cref="InMemoryDSRAuditStore"/>.
/// </summary>
public class InMemoryDSRAuditStoreTests
{
    private readonly ILogger<InMemoryDSRAuditStore> _logger;
    private readonly InMemoryDSRAuditStore _store;

    public InMemoryDSRAuditStoreTests()
    {
        _logger = Substitute.For<ILogger<InMemoryDSRAuditStore>>();
        _store = new InMemoryDSRAuditStore(_logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ShouldThrow()
    {
        // Act
        var act = () => new InMemoryDSRAuditStore(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region RecordAsync Tests

    [Fact]
    public async Task RecordAsync_ValidEntry_ShouldSucceed()
    {
        // Arrange
        var entry = CreateAuditEntry("req-001", "RequestReceived");

        // Act
        var result = await _store.RecordAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task RecordAsync_NullEntry_ShouldThrow()
    {
        // Act
        var act = async () => await _store.RecordAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task RecordAsync_MultipleEntriesForSameRequest_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await _store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified"));
        await _store.RecordAsync(CreateAuditEntry("req-001", "ErasureExecuted"));

        // Assert
        _store.Count.Should().Be(3);
    }

    [Fact]
    public async Task RecordAsync_EntriesForDifferentRequests_ShouldStoreAll()
    {
        // Arrange & Act
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await _store.RecordAsync(CreateAuditEntry("req-002", "RequestReceived"));

        // Assert
        _store.Count.Should().Be(2);
    }

    #endregion

    #region GetAuditTrailAsync Tests

    [Fact]
    public async Task GetAuditTrailAsync_ExistingRequest_ShouldReturnEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived", occurredAtUtc: new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero)));
        await _store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified", occurredAtUtc: new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero)));

        // Act
        var result = await _store.GetAuditTrailAsync("req-001");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAuditTrailAsync_ShouldReturnInChronologicalOrder()
    {
        // Arrange
        var time3 = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var time1 = new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2026, 1, 1, 11, 0, 0, TimeSpan.Zero);

        await _store.RecordAsync(CreateAuditEntry("req-001", "ErasureExecuted", occurredAtUtc: time3));
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived", occurredAtUtc: time1));
        await _store.RecordAsync(CreateAuditEntry("req-001", "IdentityVerified", occurredAtUtc: time2));

        // Act
        var result = await _store.GetAuditTrailAsync("req-001");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().HaveCount(3);
        list[0].Action.Should().Be("RequestReceived");
        list[1].Action.Should().Be("IdentityVerified");
        list[2].Action.Should().Be("ErasureExecuted");
    }

    [Fact]
    public async Task GetAuditTrailAsync_NonExistingRequest_ShouldReturnEmptyList()
    {
        // Act
        var result = await _store.GetAuditTrailAsync("non-existing");

        // Assert
        result.IsRight.Should().BeTrue();
        var list = result.RightAsEnumerable().First();
        list.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAuditTrailAsync_InvalidRequestId_ShouldThrow(string? requestId)
    {
        // Act
        var act = async () => await _store.GetAuditTrailAsync(requestId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAuditTrailAsync_MultipleRequests_ShouldIsolateTrails()
    {
        // Arrange
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await _store.RecordAsync(CreateAuditEntry("req-001", "ErasureExecuted"));
        await _store.RecordAsync(CreateAuditEntry("req-002", "RequestReceived"));

        // Act
        var result1 = await _store.GetAuditTrailAsync("req-001");
        var result2 = await _store.GetAuditTrailAsync("req-002");

        // Assert
        var list1 = result1.RightAsEnumerable().First();
        var list2 = result2.RightAsEnumerable().First();
        list1.Should().HaveCount(2);
        list2.Should().HaveCount(1);
    }

    #endregion

    #region Testing Helpers Tests

    [Fact]
    public async Task GetAllEntries_ShouldReturnAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        await _store.RecordAsync(CreateAuditEntry("req-002", "RequestReceived"));

        // Act
        var entries = _store.GetAllEntries();

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        await _store.RecordAsync(CreateAuditEntry("req-001", "RequestReceived"));
        _store.Count.Should().Be(1);

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
    }

    [Fact]
    public void Count_EmptyStore_ShouldReturnZero()
    {
        _store.Count.Should().Be(0);
    }

    #endregion

    #region Helpers

    private static DSRAuditEntry CreateAuditEntry(
        string requestId,
        string action,
        string? detail = null,
        DateTimeOffset? occurredAtUtc = null) =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            DSRRequestId = requestId,
            Action = action,
            Detail = detail,
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow
        };

    #endregion
}
