namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Guard tests for ValueObject to verify null parameter handling.
/// </summary>
public class ValueObjectGuardTests
{
    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }

        public Address(string street, string city)
        {
            Street = street;
            City = city;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
        }
    }

    private sealed class Money : SingleValueObject<decimal>
    {
        public Money(decimal value) : base(value) { }
    }

    #region ValueObject Guards

    /// <summary>
    /// Verifies that Equals handles null argument correctly.
    /// </summary>
    [Fact]
    public void Equals_NullValueObject_ReturnsFalse()
    {
        // Arrange
        var address = new Address("Main St", "NYC");
        Address? nullAddress = null;

        // Act & Assert
        address.Equals(nullAddress).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Equals(object) handles null correctly.
    /// </summary>
    [Fact]
    public void EqualsObject_NullObject_ReturnsFalse()
    {
        // Arrange
        var address = new Address("Main St", "NYC");
        object? nullObject = null;

        // Act & Assert
        address.Equals(nullObject).Should().BeFalse();
    }

    /// <summary>
    /// Verifies equality operator with null on right side.
    /// </summary>
    [Fact]
    public void EqualityOperator_RightNull_ReturnsFalse()
    {
        // Arrange
        var address = new Address("Main St", "NYC");

        // Act & Assert
        (address == null).Should().BeFalse();
    }

    /// <summary>
    /// Verifies equality operator with null on left side.
    /// </summary>
    [Fact]
    public void EqualityOperator_LeftNull_ReturnsFalse()
    {
        // Arrange
        var address = new Address("Main St", "NYC");

        // Act & Assert
        (null == address).Should().BeFalse();
    }

    /// <summary>
    /// Verifies inequality operator with null on right side.
    /// </summary>
    [Fact]
    public void InequalityOperator_RightNull_ReturnsTrue()
    {
        // Arrange
        var address = new Address("Main St", "NYC");

        // Act & Assert
        (address != null).Should().BeTrue();
    }

    /// <summary>
    /// Verifies both null returns true for equality.
    /// </summary>
    [Fact]
    public void EqualityOperator_BothNull_ReturnsTrue()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;

        // Act & Assert
        (address1 == address2).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that GetEqualityComponents can yield null values without issues.
    /// </summary>
    [Fact]
    public void GetEqualityComponents_WithNullComponent_HandlesCorrectly()
    {
        // Arrange
        var address1 = new Address("Main St", null!);
        var address2 = new Address("Main St", null!);

        // Act & Assert - should handle null components
        address1.Should().Be(address2);
    }

    #endregion

    #region SingleValueObject Guards

    /// <summary>
    /// Verifies that CompareTo handles null correctly.
    /// </summary>
    [Fact]
    public void CompareTo_NullValueObject_ReturnsPositive()
    {
        // Arrange
        var money = new Money(100m);
        Money? nullMoney = null;

        // Act
        var result = money.CompareTo(nullMoney);

        // Assert
        result.Should().BePositive();
    }

    /// <summary>
    /// Verifies implicit conversion returns the value.
    /// </summary>
    [Fact]
    public void ImplicitConversion_ReturnsValue()
    {
        // Arrange
        var money = new Money(100m);

        // Act
        decimal value = money;

        // Assert
        value.Should().Be(100m);
    }

    #endregion
}
