using FsCheck;
using FsCheck.Xunit;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using static LanguageExt.Prelude;

namespace Encina.PropertyTests.Cdc;

/// <summary>
/// Property-based tests for <see cref="InMemoryCdcPositionStore"/> invariants.
/// Verifies save/get/delete semantics and case-insensitive connector ID behavior.
/// </summary>
[Trait("Category", "Property")]
public sealed class InMemoryCdcPositionStorePropertyTests
{
    /// <summary>
    /// Sanitizes a <see cref="NonEmptyString"/> to produce a valid connector ID.
    /// FsCheck's <c>NonEmptyString</c> can generate whitespace-only strings (e.g. tab, space)
    /// which are rejected by <see cref="ArgumentException.ThrowIfNullOrWhiteSpace"/>.
    /// Returns <c>null</c> when the input cannot produce a valid connector ID.
    /// </summary>
    private static string? ToValidConnectorId(NonEmptyString input)
    {
        var value = input.Get;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    #region Save-Then-Get Round Trip

    [Property(MaxTest = 100)]
    public bool Property_SaveThenGet_ReturnsSamePosition(NonEmptyString connectorId, long positionValue)
    {
        // Property: For any connectorId, saving then getting returns the same position
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true; // Skip whitespace-only inputs (precondition)

        var store = new InMemoryCdcPositionStore();
        var position = new TestCdcPosition(positionValue);

        var saveResult = store.SavePositionAsync(id, position).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(id).GetAwaiter().GetResult();

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
    public bool Property_SaveTwiceThenGet_ReturnsLastSaved(NonEmptyString connectorId, long firstValue, long secondValue)
    {
        // Property: For any connectorId, saving twice then getting returns the last saved position
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true;

        var store = new InMemoryCdcPositionStore();
        var firstPosition = new TestCdcPosition(firstValue);
        var secondPosition = new TestCdcPosition(secondValue);

        store.SavePositionAsync(id, firstPosition).GetAwaiter().GetResult();
        store.SavePositionAsync(id, secondPosition).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(id).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == secondValue,
                None: () => false));
    }

    #endregion

    #region Delete After Save Returns None

    [Property(MaxTest = 100)]
    public bool Property_DeleteAfterSave_ReturnsNone(NonEmptyString connectorId, long positionValue)
    {
        // Property: For any connectorId, deleting after save returns None on subsequent get
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true;

        var store = new InMemoryCdcPositionStore();
        var position = new TestCdcPosition(positionValue);

        store.SavePositionAsync(id, position).GetAwaiter().GetResult();
        var deleteResult = store.DeletePositionAsync(id).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(id).GetAwaiter().GetResult();

        return deleteResult.IsRight
            && getResult.Match(
                Left: _ => false,
                Right: opt => opt.IsNone);
    }

    #endregion

    #region Case-Insensitive ConnectorId

    [Property(MaxTest = 100)]
    public bool Property_ConnectorId_IsCaseInsensitive(NonEmptyString connectorId, long positionValue)
    {
        // Property: ConnectorId comparison is case-insensitive (save with one case, get with another)
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true;

        var store = new InMemoryCdcPositionStore();
        var position = new TestCdcPosition(positionValue);
        var upperId = id.ToUpperInvariant();
        var lowerId = id.ToLowerInvariant();

        // After ToUpper/ToLower, the result might be whitespace-only for certain Unicode chars
        if (string.IsNullOrWhiteSpace(upperId) || string.IsNullOrWhiteSpace(lowerId)) return true;

        store.SavePositionAsync(upperId, position).GetAwaiter().GetResult();
        var getResult = store.GetPositionAsync(lowerId).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.Match(
                Some: p => p is TestCdcPosition tcp && tcp.Value == positionValue,
                None: () => false));
    }

    #endregion

    #region Get Without Save Returns None

    [Property(MaxTest = 100)]
    public bool Property_GetWithoutSave_ReturnsNone(NonEmptyString connectorId)
    {
        // Property: Getting a position that was never saved returns None
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true;

        var store = new InMemoryCdcPositionStore();

        var getResult = store.GetPositionAsync(id).GetAwaiter().GetResult();

        return getResult.Match(
            Left: _ => false,
            Right: opt => opt.IsNone);
    }

    #endregion

    #region Delete Without Save Succeeds

    [Property(MaxTest = 100)]
    public bool Property_DeleteWithoutSave_Succeeds(NonEmptyString connectorId)
    {
        // Property: Deleting a position that was never saved succeeds without error
        var id = ToValidConnectorId(connectorId);
        if (id is null) return true;

        var store = new InMemoryCdcPositionStore();

        var deleteResult = store.DeletePositionAsync(id).GetAwaiter().GetResult();

        return deleteResult.IsRight;
    }

    #endregion

    #region Multiple ConnectorIds Are Independent

    [Property(MaxTest = 100)]
    public bool Property_MultipleConnectors_AreIndependent(long valueA, long valueB)
    {
        // Property: Saving under different connector IDs does not interfere
        var store = new InMemoryCdcPositionStore();
        var positionA = new TestCdcPosition(valueA);
        var positionB = new TestCdcPosition(valueB);

        store.SavePositionAsync("connector-a", positionA).GetAwaiter().GetResult();
        store.SavePositionAsync("connector-b", positionB).GetAwaiter().GetResult();

        var getA = store.GetPositionAsync("connector-a").GetAwaiter().GetResult();
        var getB = store.GetPositionAsync("connector-b").GetAwaiter().GetResult();

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
