using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Errors;
using Encina.Cdc.Processing;
using Shouldly;

namespace Encina.ContractTests.Cdc;

/// <summary>
/// Contract tests verifying that any <see cref="ICdcDeadLetterStore"/> implementation
/// correctly satisfies the interface contract for adding, querying, and resolving
/// dead letter entries.
/// Uses <see cref="InMemoryCdcDeadLetterStore"/> (via internal access) as the concrete
/// implementation under test.
/// </summary>
[Trait("Category", "Contract")]
public sealed class ICdcDeadLetterStoreContractTests
{
    #region Test Helpers

    private static readonly DateTime FixedUtcNow = new(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc);

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;
        public long Value { get; }
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static InMemoryCdcDeadLetterStore CreateStore() => new();

    private static CdcDeadLetterEntry CreateEntry(
        Guid? id = null,
        string connectorId = "test-connector",
        CdcDeadLetterStatus status = CdcDeadLetterStatus.Pending)
    {
        var changeEvent = new ChangeEvent(
            TableName: "Orders",
            Operation: ChangeOperation.Insert,
            Before: null,
            After: new { Id = 1 },
            Metadata: new ChangeMetadata(
                Position: new TestCdcPosition(42),
                CapturedAtUtc: FixedUtcNow,
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: null));

        return new CdcDeadLetterEntry(
            Id: id ?? Guid.NewGuid(),
            OriginalEvent: changeEvent,
            ErrorMessage: "Test error",
            StackTrace: "at Test.Method()",
            RetryCount: 3,
            FailedAtUtc: FixedUtcNow,
            ConnectorId: connectorId,
            Status: status);
    }

    #endregion

    #region AddAsync Contract

    /// <summary>
    /// Contract: AddAsync must return Right(Unit) when adding a valid entry.
    /// </summary>
    [Fact]
    public async Task Contract_Add_ValidEntry_ReturnsSuccess()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();

        // Act
        var result = await store.AddAsync(entry);

        // Assert
        result.IsRight.ShouldBeTrue("AddAsync must return Right on success");
    }

    #endregion

    #region GetPendingAsync Contract

    /// <summary>
    /// Contract: GetPendingAsync must return only entries with <see cref="CdcDeadLetterStatus.Pending"/> status.
    /// </summary>
    [Fact]
    public async Task Contract_GetPending_ReturnsOnlyPendingEntries()
    {
        // Arrange
        var store = CreateStore();
        var pendingEntry = CreateEntry(id: Guid.NewGuid());
        var resolvedEntryId = Guid.NewGuid();
        var resolvedEntry = CreateEntry(id: resolvedEntryId);

        await store.AddAsync(pendingEntry);
        await store.AddAsync(resolvedEntry);
        await store.ResolveAsync(resolvedEntryId, CdcDeadLetterResolution.Discard);

        // Act
        var result = await store.GetPendingAsync(100);

        // Assert
        result.IsRight.ShouldBeTrue("GetPendingAsync must return Right on success");
        result.IfRight(entries =>
        {
            entries.Count.ShouldBe(1, "Only pending entries should be returned");
            entries[0].Id.ShouldBe(pendingEntry.Id,
                "The returned entry must be the one still in Pending status");
        });
    }

    /// <summary>
    /// Contract: GetPendingAsync must respect the <paramref name="maxCount"/> parameter
    /// and return no more than the requested number of entries.
    /// </summary>
    [Fact]
    public async Task Contract_GetPending_RespectsMaxCount()
    {
        // Arrange
        var store = CreateStore();
        for (var i = 0; i < 5; i++)
        {
            await store.AddAsync(CreateEntry());
        }

        // Act
        var result = await store.GetPendingAsync(3);

        // Assert
        result.IsRight.ShouldBeTrue("GetPendingAsync must return Right on success");
        result.IfRight(entries =>
            entries.Count.ShouldBeLessThanOrEqualTo(3,
                "GetPendingAsync must not return more entries than maxCount"));
    }

    /// <summary>
    /// Contract: GetPendingAsync must return an empty list when no pending entries exist.
    /// </summary>
    [Fact]
    public async Task Contract_GetPending_EmptyStore_ReturnsEmptyList()
    {
        // Arrange
        var store = CreateStore();

        // Act
        var result = await store.GetPendingAsync(10);

        // Assert
        result.IsRight.ShouldBeTrue("GetPendingAsync must return Right on success");
        result.IfRight(entries =>
            entries.Count.ShouldBe(0, "Empty store must return zero entries"));
    }

    #endregion

    #region ResolveAsync Contract

    /// <summary>
    /// Contract: ResolveAsync with <see cref="CdcDeadLetterResolution.Replay"/> must
    /// transition the entry status to <see cref="CdcDeadLetterStatus.Replayed"/>.
    /// </summary>
    [Fact]
    public async Task Contract_Resolve_Replay_TransitionsStatusToReplayed()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();
        await store.AddAsync(entry);

        // Act
        var result = await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Replay);

        // Assert
        result.IsRight.ShouldBeTrue("ResolveAsync must return Right on success");

        var pendingResult = await store.GetPendingAsync(100);
        pendingResult.IfRight(entries =>
            entries.ShouldNotContain(e => e.Id == entry.Id,
                "Replayed entry must not appear in pending results"));
    }

    /// <summary>
    /// Contract: ResolveAsync with <see cref="CdcDeadLetterResolution.Discard"/> must
    /// transition the entry status to <see cref="CdcDeadLetterStatus.Discarded"/>.
    /// </summary>
    [Fact]
    public async Task Contract_Resolve_Discard_TransitionsStatusToDiscarded()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();
        await store.AddAsync(entry);

        // Act
        var result = await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Discard);

        // Assert
        result.IsRight.ShouldBeTrue("ResolveAsync must return Right on success");

        var pendingResult = await store.GetPendingAsync(100);
        pendingResult.IfRight(entries =>
            entries.ShouldNotContain(e => e.Id == entry.Id,
                "Discarded entry must not appear in pending results"));
    }

    /// <summary>
    /// Contract: ResolveAsync must return Left with <see cref="CdcErrorCodes.DeadLetterNotFound"/>
    /// when the entry ID does not exist.
    /// </summary>
    [Fact]
    public async Task Contract_Resolve_NonExistentEntry_ReturnsNotFoundError()
    {
        // Arrange
        var store = CreateStore();
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await store.ResolveAsync(nonExistentId, CdcDeadLetterResolution.Replay);

        // Assert
        result.IsLeft.ShouldBeTrue("ResolveAsync must return Left for non-existent entry");
        result.IfLeft(error =>
            error.Message.ShouldContain(CdcErrorCodes.DeadLetterNotFound));
    }

    /// <summary>
    /// Contract: ResolveAsync must return Left with <see cref="CdcErrorCodes.DeadLetterAlreadyResolved"/>
    /// when the entry has already been resolved.
    /// </summary>
    [Fact]
    public async Task Contract_Resolve_AlreadyResolvedEntry_ReturnsAlreadyResolvedError()
    {
        // Arrange
        var store = CreateStore();
        var entry = CreateEntry();
        await store.AddAsync(entry);
        await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Replay);

        // Act
        var result = await store.ResolveAsync(entry.Id, CdcDeadLetterResolution.Discard);

        // Assert
        result.IsLeft.ShouldBeTrue("ResolveAsync must return Left for already-resolved entry");
        result.IfLeft(error =>
            error.Message.ShouldContain(CdcErrorCodes.DeadLetterAlreadyResolved));
    }

    #endregion
}
