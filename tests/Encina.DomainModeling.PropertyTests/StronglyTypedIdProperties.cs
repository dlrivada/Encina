using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for StronglyTypedId invariants.
/// </summary>
public sealed class StronglyTypedIdProperties
{
    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed class UserId : IntStronglyTypedId<UserId>
    {
        public UserId(int value) : base(value) { }
    }

    private sealed class TransactionId : LongStronglyTypedId<TransactionId>
    {
        public TransactionId(long value) : base(value) { }
    }

    #region GuidStronglyTypedId Properties

    [Property(MaxTest = 200)]
    public bool GuidId_From_PreservesValue(Guid value)
    {
        var id = OrderId.From(value);
        return id.Value == value;
    }

    [Property(MaxTest = 200)]
    public bool GuidId_ImplicitConversion_ReturnsValue(Guid value)
    {
        var id = OrderId.From(value);
        Guid converted = id;

        return converted == value;
    }

    [Property(MaxTest = 200)]
    public bool GuidId_Equality_IsReflexive(Guid value)
    {
        var id = OrderId.From(value);
        return id.Equals(id);
    }

    [Property(MaxTest = 200)]
    public bool GuidId_Equality_IsSymmetric(Guid value)
    {
        var id1 = OrderId.From(value);
        var id2 = OrderId.From(value);

        return id1.Equals(id2) == id2.Equals(id1);
    }

    [Property(MaxTest = 200)]
    public bool GuidId_WithSameValue_AreEqual(Guid value)
    {
        var id1 = OrderId.From(value);
        var id2 = OrderId.From(value);

        return id1.Equals(id2) && id1 == id2;
    }

    [Property(MaxTest = 200)]
    public bool GuidId_WithDifferentValues_AreNotEqual(Guid value1, Guid value2)
    {
        if (value1 == value2) return true;

        var id1 = OrderId.From(value1);
        var id2 = OrderId.From(value2);

        return !id1.Equals(id2) && id1 != id2;
    }

    [Property(MaxTest = 200)]
    public bool GuidId_HashCode_IsConsistent(Guid value)
    {
        var id = OrderId.From(value);
        var hash1 = id.GetHashCode();
        var hash2 = id.GetHashCode();

        return hash1 == hash2;
    }

    [Property(MaxTest = 200)]
    public bool GuidId_EqualIds_HaveSameHashCode(Guid value)
    {
        var id1 = OrderId.From(value);
        var id2 = OrderId.From(value);

        if (id1.Equals(id2))
        {
            return id1.GetHashCode() == id2.GetHashCode();
        }

        return true;
    }

    [Property(MaxTest = 100)]
    public bool GuidId_New_GeneratesValidGuid()
    {
        var id = OrderId.New();
        return id.Value != Guid.Empty;
    }

    [Property(MaxTest = 100)]
    public bool GuidId_New_GeneratesUniqueValues()
    {
        var id1 = OrderId.New();
        var id2 = OrderId.New();

        return id1.Value != id2.Value;
    }

    [Property(MaxTest = 100)]
    public bool GuidId_TryParse_ValidGuid_ReturnsId(Guid value)
    {
        var result = OrderId.TryParse(value.ToString());
        return result.IsSome && result.Match(id => id.Value == value, () => false);
    }

    [Property(MaxTest = 100)]
    public bool GuidId_TryParse_InvalidString_ReturnsNone(NonEmptyString invalidValue)
    {
        if (Guid.TryParse(invalidValue.Get, out _)) return true;

        var result = OrderId.TryParse(invalidValue.Get);
        return result.IsNone;
    }

    #endregion

    #region IntStronglyTypedId Properties

    [Property(MaxTest = 200)]
    public bool IntId_From_PreservesValue(int value)
    {
        var id = UserId.From(value);
        return id.Value == value;
    }

    [Property(MaxTest = 200)]
    public bool IntId_Equality_IsReflexive(int value)
    {
        var id = UserId.From(value);
        return id.Equals(id);
    }

    [Property(MaxTest = 200)]
    public bool IntId_WithSameValue_AreEqual(int value)
    {
        var id1 = UserId.From(value);
        var id2 = UserId.From(value);

        return id1.Equals(id2) && id1 == id2;
    }

    [Property(MaxTest = 200)]
    public bool IntId_WithDifferentValues_AreNotEqual(int value1, int value2)
    {
        if (value1 == value2) return true;

        var id1 = UserId.From(value1);
        var id2 = UserId.From(value2);

        return !id1.Equals(id2) && id1 != id2;
    }

    [Property(MaxTest = 200)]
    public bool IntId_CompareTo_IsConsistentWithValue(int value1, int value2)
    {
        var id1 = UserId.From(value1);
        var id2 = UserId.From(value2);

        var comparison = id1.CompareTo(id2);
        var expected = value1.CompareTo(value2);

        return Math.Sign(comparison) == Math.Sign(expected);
    }

    [Property(MaxTest = 200)]
    public bool IntId_CompareTo_IsAntiSymmetric(int value1, int value2)
    {
        var id1 = UserId.From(value1);
        var id2 = UserId.From(value2);

        var comp1 = id1.CompareTo(id2);
        var comp2 = id2.CompareTo(id1);

        return Math.Sign(comp1) == -Math.Sign(comp2);
    }

    [Property(MaxTest = 100)]
    public bool IntId_TryParse_ValidInt_ReturnsId(int value)
    {
        var result = UserId.TryParse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return result.IsSome && result.Match(id => id.Value == value, () => false);
    }

    #endregion

    #region LongStronglyTypedId Properties

    [Property(MaxTest = 200)]
    public bool LongId_From_PreservesValue(long value)
    {
        var id = TransactionId.From(value);
        return id.Value == value;
    }

    [Property(MaxTest = 200)]
    public bool LongId_Equality_IsReflexive(long value)
    {
        var id = TransactionId.From(value);
        return id.Equals(id);
    }

    [Property(MaxTest = 200)]
    public bool LongId_WithSameValue_AreEqual(long value)
    {
        var id1 = TransactionId.From(value);
        var id2 = TransactionId.From(value);

        return id1.Equals(id2) && id1 == id2;
    }

    [Property(MaxTest = 200)]
    public bool LongId_CompareTo_IsConsistentWithValue(long value1, long value2)
    {
        var id1 = TransactionId.From(value1);
        var id2 = TransactionId.From(value2);

        var comparison = id1.CompareTo(id2);
        var expected = value1.CompareTo(value2);

        return Math.Sign(comparison) == Math.Sign(expected);
    }

    [Property(MaxTest = 100)]
    public bool LongId_TryParse_ValidLong_ReturnsId(long value)
    {
        var result = TransactionId.TryParse(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return result.IsSome && result.Match(id => id.Value == value, () => false);
    }

    #endregion
}
