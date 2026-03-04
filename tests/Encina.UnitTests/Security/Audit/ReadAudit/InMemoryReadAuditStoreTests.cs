using Encina.Security.Audit;
using FluentAssertions;

namespace Encina.UnitTests.Security.Audit.ReadAudit;

/// <summary>
/// Unit tests for <see cref="InMemoryReadAuditStore"/>.
/// </summary>
public class InMemoryReadAuditStoreTests
{
    private readonly InMemoryReadAuditStore _store;

    public InMemoryReadAuditStoreTests()
    {
        _store = new InMemoryReadAuditStore();
    }

    #region LogReadAsync Tests

    [Fact]
    public async Task LogReadAsync_ValidEntry_ShouldAddToStore()
    {
        // Arrange
        var entry = CreateTestEntry();

        // Act
        var result = await _store.LogReadAsync(entry);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task LogReadAsync_DuplicateId_ShouldUpdateExisting()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entry1 = CreateTestEntry(id: id, entityType: "Patient");
        var entry2 = CreateTestEntry(id: id, entityType: "FinancialRecord");

        // Act
        await _store.LogReadAsync(entry1);
        var result = await _store.LogReadAsync(entry2);

        // Assert
        result.IsRight.Should().BeTrue();
        _store.Count.Should().Be(1);
        var stored = _store.GetAllEntries()[0];
        stored.EntityType.Should().Be("FinancialRecord");
    }

    [Fact]
    public async Task LogReadAsync_NullEntry_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _store.LogReadAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entry");
    }

    [Fact]
    public async Task LogReadAsync_CancelledToken_ShouldReturnError()
    {
        // Arrange
        var entry = CreateTestEntry();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act
        var result = await _store.LogReadAsync(entry, cts.Token);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task LogReadAsync_ConcurrentWrites_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _store.LogReadAsync(CreateTestEntry()).AsTask())
            .ToList();

        // Act
        await Task.WhenAll(tasks);

        // Assert
        _store.Count.Should().Be(100);
    }

    #endregion

    #region GetAccessHistoryAsync Tests

    [Fact]
    public async Task GetAccessHistoryAsync_WithMatchingEntries_ShouldReturnFiltered()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P-1"));
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P-2"));
        await _store.LogReadAsync(CreateTestEntry(entityType: "Order", entityId: "O-1"));

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "P-1");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(1);
                entries[0].EntityId.Should().Be("P-1");
                return true;
            },
            Left: _ => false).Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P-1"));

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "P-999");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Count,
            Left: _ => -1).Should().Be(0);
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ShouldBeCaseInsensitiveOnEntityType()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient", entityId: "P-1"));

        // Act
        var result = await _store.GetAccessHistoryAsync("patient", "P-1");

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Count,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task GetAccessHistoryAsync_ShouldOrderByAccessedAtUtcDescending()
    {
        // Arrange
        var older = CreateTestEntry(entityType: "Patient", entityId: "P-1",
            accessedAtUtc: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = CreateTestEntry(entityType: "Patient", entityId: "P-1",
            accessedAtUtc: DateTimeOffset.UtcNow);

        await _store.LogReadAsync(older);
        await _store.LogReadAsync(newer);

        // Act
        var result = await _store.GetAccessHistoryAsync("Patient", "P-1");

        // Assert
        result.Match(
            Right: entries =>
            {
                entries.Should().HaveCount(2);
                entries[0].AccessedAtUtc.Should().BeOnOrAfter(entries[1].AccessedAtUtc);
                return true;
            },
            Left: _ => false).Should().BeTrue();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityType_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _store.GetAccessHistoryAsync(null!, "id");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAccessHistoryAsync_NullEntityId_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _store.GetAccessHistoryAsync("Patient", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region GetUserAccessHistoryAsync Tests

    [Fact]
    public async Task GetUserAccessHistoryAsync_WithMatchingEntries_ShouldReturnFiltered()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1", accessedAtUtc: now));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-2", accessedAtUtc: now));

        // Act
        var result = await _store.GetUserAccessHistoryAsync(
            "user-1", now.AddMinutes(-1), now.AddMinutes(1));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Count,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_OutsideDateRange_ShouldReturnEmpty()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1", accessedAtUtc: now.AddDays(-10)));

        // Act
        var result = await _store.GetUserAccessHistoryAsync(
            "user-1", now.AddDays(-1), now);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: entries => entries.Count,
            Left: _ => -1).Should().Be(0);
    }

    [Fact]
    public async Task GetUserAccessHistoryAsync_NullUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = async () => await _store.GetUserAccessHistoryAsync(
            null!, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region QueryAsync Tests

    [Fact]
    public async Task QueryAsync_WithNoFilters_ShouldReturnAllEntries()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await _store.LogReadAsync(CreateTestEntry());
        }

        var query = new ReadAuditQuery();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(5);
                paged.Items.Should().HaveCount(5);
                return true;
            },
            Left: _ => false).Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WithUserIdFilter_ShouldReturnMatchingEntries()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-2"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1"));

        var query = ReadAuditQuery.Builder().ForUser("user-1").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(2);
    }

    [Fact]
    public async Task QueryAsync_WithEntityTypeFilter_ShouldBeCaseInsensitive()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(entityType: "Patient"));
        await _store.LogReadAsync(CreateTestEntry(entityType: "Order"));

        var query = ReadAuditQuery.Builder().ForEntityType("patient").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithAccessMethodFilter_ShouldReturnMatching()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(accessMethod: ReadAccessMethod.Repository));
        await _store.LogReadAsync(CreateTestEntry(accessMethod: ReadAccessMethod.Export));

        var query = ReadAuditQuery.Builder()
            .WithAccessMethod(ReadAccessMethod.Export)
            .Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithPurposeFilter_ShouldBeCaseInsensitiveContains()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(purpose: "Patient Care Review"));
        await _store.LogReadAsync(CreateTestEntry(purpose: "Financial Audit"));

        var query = ReadAuditQuery.Builder().WithPurpose("patient care").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithDateRange_ShouldReturnInRange()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now.AddDays(-5)));
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now));
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now.AddDays(5)));

        var query = ReadAuditQuery.Builder()
            .InDateRange(now.AddDays(-1), now.AddDays(1))
            .Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        for (var i = 0; i < 25; i++)
        {
            await _store.LogReadAsync(CreateTestEntry());
        }

        var query = ReadAuditQuery.Builder()
            .OnPage(2)
            .WithPageSize(10)
            .Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged =>
            {
                paged.TotalCount.Should().Be(25);
                paged.Items.Should().HaveCount(10);
                paged.PageNumber.Should().Be(2);
                paged.PageSize.Should().Be(10);
                paged.TotalPages.Should().Be(3);
                paged.HasPreviousPage.Should().BeTrue();
                paged.HasNextPage.Should().BeTrue();
                return true;
            },
            Left: _ => false).Should().BeTrue();
    }

    [Fact]
    public async Task QueryAsync_WithTenantIdFilter_ShouldReturnMatching()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(tenantId: "tenant-A"));
        await _store.LogReadAsync(CreateTestEntry(tenantId: "tenant-B"));

        var query = ReadAuditQuery.Builder().ForTenant("tenant-A").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithCorrelationIdFilter_ShouldReturnMatching()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(correlationId: "corr-1"));
        await _store.LogReadAsync(CreateTestEntry(correlationId: "corr-2"));

        var query = ReadAuditQuery.Builder().WithCorrelationId("corr-1").Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_WithMultipleFilters_ShouldApplyAll()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1", entityType: "Patient"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-1", entityType: "Order"));
        await _store.LogReadAsync(CreateTestEntry(userId: "user-2", entityType: "Patient"));

        var query = ReadAuditQuery.Builder()
            .ForUser("user-1")
            .ForEntityType("Patient")
            .Build();

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged => paged.TotalCount,
            Left: _ => -1).Should().Be(1);
    }

    [Fact]
    public async Task QueryAsync_NullQuery_ShouldThrowArgumentNullException()
    {
        // Act
        var act = async () => await _store.QueryAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("query");
    }

    [Fact]
    public async Task QueryAsync_PageSizeExceedsMax_ShouldClampToMax()
    {
        // Arrange
        for (var i = 0; i < 5; i++)
        {
            await _store.LogReadAsync(CreateTestEntry());
        }

        var query = new ReadAuditQuery { PageSize = 9999 };

        // Act
        var result = await _store.QueryAsync(query);

        // Assert
        result.Match(
            Right: paged =>
            {
                paged.PageSize.Should().BeLessThanOrEqualTo(ReadAuditQuery.MaxPageSize);
                return true;
            },
            Left: _ => false).Should().BeTrue();
    }

    #endregion

    #region PurgeEntriesAsync Tests

    [Fact]
    public async Task PurgeEntriesAsync_ShouldRemoveOldEntries()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now.AddDays(-10)));
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now.AddDays(-5)));
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: now));

        // Act
        var result = await _store.PurgeEntriesAsync(now.AddDays(-3));

        // Assert
        result.IsRight.Should().BeTrue();
        result.Match(
            Right: count => count,
            Left: _ => -1).Should().Be(2);
        _store.Count.Should().Be(1);
    }

    [Fact]
    public async Task PurgeEntriesAsync_NoOldEntries_ShouldReturnZero()
    {
        // Arrange
        await _store.LogReadAsync(CreateTestEntry(accessedAtUtc: DateTimeOffset.UtcNow));

        // Act
        var result = await _store.PurgeEntriesAsync(DateTimeOffset.UtcNow.AddDays(-1));

        // Assert
        result.Match(
            Right: count => count,
            Left: _ => -1).Should().Be(0);
        _store.Count.Should().Be(1);
    }

    #endregion

    #region Test Helpers

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        // Arrange
        _store.LogReadAsync(CreateTestEntry()).AsTask().GetAwaiter().GetResult();
        _store.LogReadAsync(CreateTestEntry()).AsTask().GetAwaiter().GetResult();

        // Act
        _store.Clear();

        // Assert
        _store.Count.Should().Be(0);
        _store.GetAllEntries().Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private static ReadAuditEntry CreateTestEntry(
        Guid? id = null,
        string entityType = "Patient",
        string? entityId = "P-12345",
        string? userId = "user-1",
        string? tenantId = null,
        DateTimeOffset? accessedAtUtc = null,
        string? correlationId = null,
        string? purpose = null,
        ReadAccessMethod accessMethod = ReadAccessMethod.Repository,
        int entityCount = 1) => new()
        {
            Id = id ?? Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            TenantId = tenantId,
            AccessedAtUtc = accessedAtUtc ?? DateTimeOffset.UtcNow,
            CorrelationId = correlationId,
            Purpose = purpose,
            AccessMethod = accessMethod,
            EntityCount = entityCount
        };

    #endregion
}
