using Encina.DomainModeling;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Property-based tests for ValueObject equality and immutability invariants.
/// </summary>
public sealed class ValueObjectProperties
{
    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string PostalCode { get; }

        public Address(string street, string city, string postalCode)
        {
            Street = street;
            City = city;
            PostalCode = postalCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return PostalCode;
        }
    }

    private sealed class Money : SingleValueObject<decimal>
    {
        public Money(decimal value) : base(value) { }
    }

    #region ValueObject Equality Properties

    [Property(MaxTest = 200)]
    public bool ValueObject_Equality_IsReflexive(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address = new Address(street.Get, city.Get, postal.Get);
        return address.Equals(address);
    }

    [Property(MaxTest = 200)]
    public bool ValueObject_Equality_IsSymmetric(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address1 = new Address(street.Get, city.Get, postal.Get);
        var address2 = new Address(street.Get, city.Get, postal.Get);

        return address1.Equals(address2) == address2.Equals(address1);
    }

    [Property(MaxTest = 200)]
    public bool ValueObject_Equality_IsTransitive(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address1 = new Address(street.Get, city.Get, postal.Get);
        var address2 = new Address(street.Get, city.Get, postal.Get);
        var address3 = new Address(street.Get, city.Get, postal.Get);

        if (address1.Equals(address2) && address2.Equals(address3))
        {
            return address1.Equals(address3);
        }

        return true;
    }

    [Property(MaxTest = 200)]
    public bool ValueObject_WithSameComponents_AreEqual(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address1 = new Address(street.Get, city.Get, postal.Get);
        var address2 = new Address(street.Get, city.Get, postal.Get);

        return address1.Equals(address2) && address1 == address2;
    }

    [Property(MaxTest = 200)]
    public bool ValueObject_WithDifferentComponents_AreNotEqual(
        NonEmptyString street1,
        NonEmptyString street2,
        NonEmptyString city,
        NonEmptyString postal)
    {
        if (street1.Get == street2.Get) return true;

        var address1 = new Address(street1.Get, city.Get, postal.Get);
        var address2 = new Address(street2.Get, city.Get, postal.Get);

        return !address1.Equals(address2) && address1 != address2;
    }

    #endregion

    #region ValueObject HashCode Properties

    [Property(MaxTest = 200)]
    public bool ValueObject_HashCode_IsConsistent(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address = new Address(street.Get, city.Get, postal.Get);
        var hash1 = address.GetHashCode();
        var hash2 = address.GetHashCode();

        return hash1 == hash2;
    }

    [Property(MaxTest = 200)]
    public bool ValueObject_EqualObjects_HaveSameHashCode(NonEmptyString street, NonEmptyString city, NonEmptyString postal)
    {
        var address1 = new Address(street.Get, city.Get, postal.Get);
        var address2 = new Address(street.Get, city.Get, postal.Get);

        if (address1.Equals(address2))
        {
            return address1.GetHashCode() == address2.GetHashCode();
        }

        return true;
    }

    #endregion

    #region SingleValueObject Properties

    [Property(MaxTest = 200)]
    public bool SingleValueObject_Value_IsPreserved(decimal value)
    {
        var money = new Money(value);
        return money.Value == value;
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_ImplicitConversion_ReturnsValue(decimal value)
    {
        var money = new Money(value);
        decimal converted = money;

        return converted == value;
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_Equality_IsReflexive(decimal value)
    {
        var money = new Money(value);
        return money.Equals(money);
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_Equality_IsSymmetric(decimal value)
    {
        var money1 = new Money(value);
        var money2 = new Money(value);

        return money1.Equals(money2) == money2.Equals(money1);
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_WithSameValue_AreEqual(decimal value)
    {
        var money1 = new Money(value);
        var money2 = new Money(value);

        return money1.Equals(money2) && money1 == money2;
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_WithDifferentValues_AreNotEqual(decimal value1, decimal value2)
    {
        if (value1 == value2) return true;

        var money1 = new Money(value1);
        var money2 = new Money(value2);

        return !money1.Equals(money2) && money1 != money2;
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_CompareTo_IsConsistentWithValue(decimal value1, decimal value2)
    {
        var money1 = new Money(value1);
        var money2 = new Money(value2);

        var comparison = money1.CompareTo(money2);
        var expected = value1.CompareTo(value2);

        return Math.Sign(comparison) == Math.Sign(expected);
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_CompareTo_IsAntiSymmetric(decimal value1, decimal value2)
    {
        var money1 = new Money(value1);
        var money2 = new Money(value2);

        var comp1 = money1.CompareTo(money2);
        var comp2 = money2.CompareTo(money1);

        return Math.Sign(comp1) == -Math.Sign(comp2);
    }

    [Property(MaxTest = 200)]
    public bool SingleValueObject_CompareTo_IsTransitive(decimal value1, decimal value2, decimal value3)
    {
        var money1 = new Money(value1);
        var money2 = new Money(value2);
        var money3 = new Money(value3);

        if (money1.CompareTo(money2) <= 0 && money2.CompareTo(money3) <= 0)
        {
            return money1.CompareTo(money3) <= 0;
        }

        return true;
    }

    #endregion
}
