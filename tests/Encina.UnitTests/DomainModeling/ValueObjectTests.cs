using Encina.DomainModeling;

namespace Encina.UnitTests.DomainModeling;

public class ValueObjectTests
{
    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string Country { get; }

        public Address(string street, string city, string country)
        {
            Street = street;
            City = city;
            Country = country;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return Country;
        }
    }

    private sealed class Email : SingleValueObject<string>
    {
        public Email(string value) : base(value) { }
    }

    [Fact]
    public void ValueObject_WithSameComponents_ShouldBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA");
        var address2 = new Address("123 Main St", "New York", "USA");

        // Act & Assert
        address1.ShouldBe(address2);
        address1.Equals(address2).ShouldBeTrue();
        (address1 == address2).ShouldBeTrue();
        (address1 != address2).ShouldBeFalse();
    }

    [Fact]
    public void ValueObject_WithDifferentComponents_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA");
        var address2 = new Address("456 Oak Ave", "Los Angeles", "USA");

        // Act & Assert
        address1.ShouldNotBe(address2);
        address1.Equals(address2).ShouldBeFalse();
        (address1 == address2).ShouldBeFalse();
        (address1 != address2).ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_WithOneDifferentComponent_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA");
        var address2 = new Address("123 Main St", "Boston", "USA");

        // Act & Assert
        address1.ShouldNotBe(address2);
    }

    [Fact]
    public void ValueObject_ComparedWithNull_ShouldNotBeEqual()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "USA");

        // Act & Assert
        address.Equals(null).ShouldBeFalse();
        (address == null).ShouldBeFalse();
        (address != null).ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_SameReference_ShouldBeEqual()
    {
        // Arrange
        var address = new Address("123 Main St", "New York", "USA");

        // Act & Assert
        address.Equals(address).ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_GetHashCode_ShouldBeConsistentForSameComponents()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA");
        var address2 = new Address("123 Main St", "New York", "USA");

        // Act & Assert
        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }

    [Fact]
    public void ValueObject_GetHashCode_ShouldDifferForDifferentComponents()
    {
        // Arrange
        var address1 = new Address("123 Main St", "New York", "USA");
        var address2 = new Address("456 Oak Ave", "Los Angeles", "USA");

        // Act & Assert
        address1.GetHashCode().ShouldNotBe(address2.GetHashCode());
    }

    [Fact]
    public void SingleValueObject_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("test@example.com");

        // Act & Assert
        email1.ShouldBe(email2);
        email1.Equals(email2).ShouldBeTrue();
    }

    [Fact]
    public void SingleValueObject_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = new Email("test@example.com");
        var email2 = new Email("other@example.com");

        // Act & Assert
        email1.ShouldNotBe(email2);
    }

    [Fact]
    public void SingleValueObject_Value_ShouldBeAccessible()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act & Assert
        email.Value.ShouldBe("test@example.com");
    }

    [Fact]
    public void SingleValueObject_ToString_ShouldReturnValue()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act & Assert
        email.ToString().ShouldBe("test@example.com");
    }

    [Fact]
    public void SingleValueObject_ImplicitConversion_ShouldWork()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act
        string value = email;

        // Assert
        value.ShouldBe("test@example.com");
    }

    [Fact]
    public void SingleValueObject_CompareTo_ShouldCompareByValue()
    {
        // Arrange
        var email1 = new Email("a@example.com");
        var email2 = new Email("b@example.com");
        var email3 = new Email("a@example.com");

        // Act & Assert
        email1.CompareTo(email2).ShouldBeLessThan(0);
        email2.CompareTo(email1).ShouldBeGreaterThan(0);
        email1.CompareTo(email3).ShouldBe(0);
    }

    [Fact]
    public void SingleValueObject_CompareTo_Null_ShouldReturnPositive()
    {
        // Arrange
        var email = new Email("test@example.com");

        // Act & Assert
        email.CompareTo(null).ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NullValueObjects_ShouldBeEqual()
    {
        // Arrange
        Address? address1 = null;
        Address? address2 = null;

        // Act & Assert
        (address1 == address2).ShouldBeTrue();
        (address1 != address2).ShouldBeFalse();
    }
}
