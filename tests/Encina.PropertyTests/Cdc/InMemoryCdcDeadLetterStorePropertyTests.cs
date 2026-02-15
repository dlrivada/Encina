using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.DeadLetter;
using Encina.Cdc.Processing;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc;

/// <summary>
/// Property-based tests for <see cref="InMemoryCdcDeadLetterStore"/> invariants.
/// Verifies that store operations maintain consistency under varied inputs.
/// </summary>
[Trait("Category", "Property")]
public sealed class InMemoryCdcDeadLetterStorePropertyTests
{
    #region Test Helpers

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;
        public long Value { get; }
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static CdcDeadLetterEntry CreateEntry(Guid? id = null, string connectorId = "test-connector")
    {
        var changeEvent = new ChangeEvent(
            TableName: "Orders",
            Operation: ChangeOperation.Insert,
            Before: null,
            After: new { Id = 1 },
            Metadata: new ChangeMetadata(
                Position: new TestCdcPosition(42),
                CapturedAtUtc: new DateTime(2026, 2, 15, 12, 0, 0, DateTimeKind.Utc),
                TransactionId: null,
                SourceDatabase: null,
                SourceSchema: null));

        return new CdcDeadLetterEntry(
            Id: id ?? Guid.NewGuid(),
            OriginalEvent: changeEvent,
            ErrorMessage: "Test error",
            StackTrace: "at Test.Method()",
            RetryCount: 3,
            FailedAtUtc: DateTime.UtcNow,
            ConnectorId: connectorId,
            Status: CdcDeadLetterStatus.Pending);
    }

    #endregion

    #region Add-then-Get Round Trip

    /// <summary>
    /// Property: All entries added with Pending status are retrievable via GetPendingAsync.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_AddThenGetPending_AllAddedEntriesAreRetrievable(PositiveInt count)
    {
        var entryCount = Math.Min(count.Get, 50); // Cap to avoid slow test
        var store = new InMemoryCdcDeadLetterStore();
        var ids = new List<Guid>();

        for (var i = 0; i < entryCount; i++)
        {
            var entry = CreateEntry();
            ids.Add(entry.Id);
            store.AddAsync(entry).GetAwaiter().GetResult();
        }

        var result = store.GetPendingAsync(entryCount + 10).GetAwaiter().GetResult();

        return result.Match(
            Right: entries => entries.Count == entryCount && ids.All(id => entries.Any(e => e.Id == id)),
            Left: _ => false);
    }

    #endregion

    #region Resolved Entries Not In Pending

    /// <summary>
    /// Property: Resolved entries never appear in GetPendingAsync results.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolvedEntries_NeverAppearInPending(bool useReplay)
    {
        var store = new InMemoryCdcDeadLetterStore();
        var entry = CreateEntry();
        var resolution = useReplay ? CdcDeadLetterResolution.Replay : CdcDeadLetterResolution.Discard;

        store.AddAsync(entry).GetAwaiter().GetResult();
        store.ResolveAsync(entry.Id, resolution).GetAwaiter().GetResult();

        var result = store.GetPendingAsync(100).GetAwaiter().GetResult();

        return result.Match(
            Right: entries => !entries.Any(e => e.Id == entry.Id),
            Left: _ => false);
    }

    #endregion

    #region Resolve Idempotency

    /// <summary>
    /// Property: Resolving an already-resolved entry returns an error consistently.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_ResolveAlreadyResolved_AlwaysReturnsError(bool firstReplay, bool secondReplay)
    {
        var store = new InMemoryCdcDeadLetterStore();
        var entry = CreateEntry();
        var firstResolution = firstReplay ? CdcDeadLetterResolution.Replay : CdcDeadLetterResolution.Discard;
        var secondResolution = secondReplay ? CdcDeadLetterResolution.Replay : CdcDeadLetterResolution.Discard;

        store.AddAsync(entry).GetAwaiter().GetResult();
        store.ResolveAsync(entry.Id, firstResolution).GetAwaiter().GetResult();

        var secondResult = store.ResolveAsync(entry.Id, secondResolution).GetAwaiter().GetResult();

        return secondResult.IsLeft;
    }

    #endregion

    #region Entry Count Invariant

    /// <summary>
    /// Property: The count of pending entries never exceeds the total number of added entries.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_PendingCount_NeverExceedsTotalAdded(PositiveInt addCount, PositiveInt resolveCount)
    {
        var totalAdd = Math.Min(addCount.Get, 50);
        var totalResolve = Math.Min(resolveCount.Get, totalAdd);
        var store = new InMemoryCdcDeadLetterStore();
        var entries = new List<CdcDeadLetterEntry>();

        for (var i = 0; i < totalAdd; i++)
        {
            var entry = CreateEntry();
            entries.Add(entry);
            store.AddAsync(entry).GetAwaiter().GetResult();
        }

        for (var i = 0; i < totalResolve; i++)
        {
            store.ResolveAsync(entries[i].Id, CdcDeadLetterResolution.Discard).GetAwaiter().GetResult();
        }

        var result = store.GetPendingAsync(totalAdd + 10).GetAwaiter().GetResult();

        return result.Match(
            Right: pending => pending.Count <= totalAdd && pending.Count == totalAdd - totalResolve,
            Left: _ => false);
    }

    #endregion

    #region Connector Isolation

    /// <summary>
    /// Property: Entries from different connectors are independently stored.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool Property_DifferentConnectors_AreIndependent(PositiveInt countA, PositiveInt countB)
    {
        var nA = Math.Min(countA.Get, 20);
        var nB = Math.Min(countB.Get, 20);
        var store = new InMemoryCdcDeadLetterStore();

        for (var i = 0; i < nA; i++)
        {
            store.AddAsync(CreateEntry(connectorId: "connector-a")).GetAwaiter().GetResult();
        }

        for (var i = 0; i < nB; i++)
        {
            store.AddAsync(CreateEntry(connectorId: "connector-b")).GetAwaiter().GetResult();
        }

        var result = store.GetPendingAsync(nA + nB + 10).GetAwaiter().GetResult();

        return result.Match(
            Right: entries => entries.Count == nA + nB,
            Left: _ => false);
    }

    #endregion
}
