using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Cdc.Sharding;

/// <summary>
/// Property-based tests for <see cref="InMemoryShardedCdcPositionStore"/> invariants.
/// Verifies save/get/delete semantics with composite keys and case-insensitive behavior.
/// </summary>
[Trait("Category", "Property")]
public sealed class InMemoryShardedCdcPositionStorePropertyTests
{
    /// <summary>
    /// Sanitizes a <see cref="NonEmptyString"/> to produce a valid identifier.
    /// FsCheck's <c>NonEmptyString</c> can generate whitespace-only strings
    /// which are rejected by <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
    /// Returns <c>null</c> when the input cannot produce a valid identifier.
    /// </summary>
    private static string? ToValidId(NonEmptyString input)
    {
        var value = input.Get;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    #region Save-Then-Get Round Trip

    [Property(MaxTest = 100)]
    public bool Property_SaveThenGet_ReturnsSamePosition(
        NonEmptyString shardId,
        NonEmptyString connectorId,
        long positionValue)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true; // Skip invalid inputs

        var store = new InMemoryShardedCdcPositionStore();
        var position = new TestCdcPosition(positionValue);

        var saveResult = store.SavePositionAsync(sid, cid, position).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(sid, cid).GetAwaiter().GetResult();

        return saveResult.IsRight
            && getResult.Match(
                Left: _ => false,
                Right: opt => opt.Match(
                    Some: p => p is TestCdcPosition tcp && tcp.Value == positionValue,
                    None: () => false));
    }

    #endregion

    #region Save-Twice-Then-Get Returns Last

    [Property(MaxTest = 100)]
    public bool Property_SaveTwiceThenGet_ReturnsLastSaved(
        NonEmptyString shardId,
        NonEmptyString connectorId,
        long firstValue,
        long secondValue)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true;

        var store = new InMemoryShardedCdcPositionStore();

        store.SavePositionAsync(sid, cid, new TestCdcPosition(firstValue)).GetAwaiter().GetResult();
        store.SavePositionAsync(sid, cid, new TestCdcPosition(secondValue)).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(sid, cid).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == secondValue,
                None: () => false));
    }

    #endregion

    #region Delete After Save Returns None

    [Property(MaxTest = 100)]
    public bool Property_DeleteAfterSave_ReturnsNone(
        NonEmptyString shardId,
        NonEmptyString connectorId,
        long positionValue)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true;

        var store = new InMemoryShardedCdcPositionStore();

        store.SavePositionAsync(sid, cid, new TestCdcPosition(positionValue)).GetAwaiter().GetResult();
        var deleteResult = store.DeletePositionAsync(sid, cid).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(sid, cid).GetAwaiter().GetResult();

        return deleteResult.IsRight
            && getResult.Match(
                Left: _ => false,
                Right: opt => opt.IsNone);
    }

    #endregion

    #region Case-Insensitive Composite Key

    [Property(MaxTest = 100)]
    public bool Property_CompositeKey_IsCaseInsensitive(
        NonEmptyString shardId,
        NonEmptyString connectorId,
        long positionValue)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true;

        var upperSid = sid.ToUpperInvariant();
        var lowerSid = sid.ToLowerInvariant();
        var upperCid = cid.ToUpperInvariant();
        var lowerCid = cid.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(upperSid) || string.IsNullOrWhiteSpace(lowerSid)
            || string.IsNullOrWhiteSpace(upperCid) || string.IsNullOrWhiteSpace(lowerCid))
            return true;

        var store = new InMemoryShardedCdcPositionStore();

        store.SavePositionAsync(upperSid, upperCid, new TestCdcPosition(positionValue))
            .GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(lowerSid, lowerCid).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == positionValue,
                None: () => false));
    }

    #endregion

    #region Get Without Save Returns None

    [Property(MaxTest = 100)]
    public bool Property_GetWithoutSave_ReturnsNone(
        NonEmptyString shardId,
        NonEmptyString connectorId)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true;

        var store = new InMemoryShardedCdcPositionStore();

        var getResult = store.GetPositionAsync(sid, cid).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.IsNone);
    }

    #endregion

    #region Delete Without Save Succeeds

    [Property(MaxTest = 100)]
    public bool Property_DeleteWithoutSave_Succeeds(
        NonEmptyString shardId,
        NonEmptyString connectorId)
    {
        var sid = ToValidId(shardId);
        var cid = ToValidId(connectorId);
        if (sid is null || cid is null) return true;

        var store = new InMemoryShardedCdcPositionStore();

        var deleteResult = store.DeletePositionAsync(sid, cid).GetAwaiter().GetResult();

        return deleteResult.IsRight;
    }

    #endregion

    #region Composite Key Independence

    [Property(MaxTest = 100)]
    public bool Property_DifferentShards_SameConnector_AreIndependent(long valueA, long valueB)
    {
        var store = new InMemoryShardedCdcPositionStore();

        store.SavePositionAsync("shard-a", "connector-1", new TestCdcPosition(valueA))
            .GetAwaiter().GetResult();
        store.SavePositionAsync("shard-b", "connector-1", new TestCdcPosition(valueB))
            .GetAwaiter().GetResult();

        var getA = store.GetPositionAsync("shard-a", "connector-1").GetAwaiter().GetResult();
        var getB = store.GetPositionAsync("shard-b", "connector-1").GetAwaiter().GetResult();

        var aCorrect = getA.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == valueA,
                None: () => false));

        var bCorrect = getB.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == valueB,
                None: () => false));

        return aCorrect && bCorrect;
    }

    [Property(MaxTest = 100)]
    public bool Property_SameShard_DifferentConnectors_AreIndependent(long valueA, long valueB)
    {
        var store = new InMemoryShardedCdcPositionStore();

        store.SavePositionAsync("shard-1", "connector-a", new TestCdcPosition(valueA))
            .GetAwaiter().GetResult();
        store.SavePositionAsync("shard-1", "connector-b", new TestCdcPosition(valueB))
            .GetAwaiter().GetResult();

        var getA = store.GetPositionAsync("shard-1", "connector-a").GetAwaiter().GetResult();
        var getB = store.GetPositionAsync("shard-1", "connector-b").GetAwaiter().GetResult();

        var aCorrect = getA.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == valueA,
                None: () => false));

        var bCorrect = getB.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == valueB,
                None: () => false));

        return aCorrect && bCorrect;
    }

    #endregion

    #region GetAllPositionsAsync Returns Correct Count

    [Property(MaxTest = 50)]
    public bool Property_GetAllPositions_ReturnsCorrectCountForConnector(PositiveInt shardCount)
    {
        var count = Math.Min(shardCount.Get, 20); // Limit to reasonable size
        var store = new InMemoryShardedCdcPositionStore();

        for (var i = 0; i < count; i++)
        {
            store.SavePositionAsync($"shard-{i}", "test-connector", new TestCdcPosition(i))
                .GetAwaiter().GetResult();
        }

        // Also save some positions for a different connector
        store.SavePositionAsync("other-shard", "other-connector", new TestCdcPosition(999))
            .GetAwaiter().GetResult();

        var result = store.GetAllPositionsAsync("test-connector").GetAwaiter().GetResult();

        return result.Match(
            Left: _ => false,
            Right: positions => positions.Count == count);
    }

    #endregion

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;

        public long Value { get; }

        public override byte[] ToBytes() => BitConverter.GetBytes(Value);

        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
