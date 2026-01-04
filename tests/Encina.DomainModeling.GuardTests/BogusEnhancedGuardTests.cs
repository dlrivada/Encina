using Bogus;
using Encina.DomainModeling.GuardTests.Fakers;

namespace Encina.DomainModeling.GuardTests;

/// <summary>
/// Enhanced guard tests using Bogus for realistic test data generation.
/// </summary>
public class BogusEnhancedGuardTests
{
    #region ValueObject Guards with Bogus

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

    [Fact]
    public void ValueObject_WithBogusData_EqualsNull_ReturnsFalse()
    {
        // Arrange
        var data = GuardTestFakers.AddressDataFaker.Generate();
        var address = new Address(data.Street, data.City);
        Address? nullAddress = null;

        // Act & Assert
        address.Equals(nullAddress).ShouldBeFalse();
    }

    [Fact]
    public void ValueObject_WithBogusData_EqualityOperatorWithNull_Works()
    {
        // Arrange
        var data = GuardTestFakers.AddressDataFaker.Generate();
        var address = new Address(data.Street, data.City);

        // Act & Assert
        (address == null).ShouldBeFalse();
        (null == address).ShouldBeFalse();
        (address != null).ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_WithBogusData_SameDataEquals()
    {
        // Arrange
        var data = GuardTestFakers.AddressDataFaker.Generate();
        var address1 = new Address(data.Street, data.City);
        var address2 = new Address(data.Street, data.City);

        // Act & Assert
        address1.ShouldBe(address2);
        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }

    [Fact]
    public void ValueObject_WithBogusData_DifferentDataNotEquals()
    {
        // Arrange - use different seeds to guarantee distinct data deterministically
        var faker1 = GuardTestFakers.CreateAddressDataFaker().UseSeed(1);
        var faker2 = GuardTestFakers.CreateAddressDataFaker().UseSeed(2);

        var data1 = faker1.Generate();
        var data2 = faker2.Generate();

        var address1 = new Address(data1.Street, data1.City);
        var address2 = new Address(data2.Street, data2.City);

        // Act & Assert - unconditionally verify inequality
        address1.ShouldNotBe(address2);
    }

    #endregion

    #region Entity Guards with Bogus

    private sealed class Customer : Entity<Guid>
    {
        public string Name { get; }

        public Customer(Guid id, string name) : base(id)
        {
            Name = name;
        }
    }

    [Fact]
    public void Entity_WithBogusData_EqualsNull_ReturnsFalse()
    {
        // Arrange
        var data = GuardTestFakers.EntityDataFaker.Generate();
        var customer = new Customer(data.Id, data.Name);
        Customer? nullCustomer = null;

        // Act & Assert
        customer.Equals(nullCustomer).ShouldBeFalse();
    }

    [Fact]
    public void Entity_WithBogusData_EqualityOperatorWithNull_Works()
    {
        // Arrange
        var data = GuardTestFakers.EntityDataFaker.Generate();
        var customer = new Customer(data.Id, data.Name);

        // Act & Assert
        (customer == null).ShouldBeFalse();
        (null == customer).ShouldBeFalse();
        (customer != null).ShouldBeTrue();
    }

    [Fact]
    public void Entity_WithBogusData_SameIdEquals()
    {
        // Arrange
        var data1 = GuardTestFakers.EntityDataFaker.Generate();
        var data2 = GuardTestFakers.EntityDataFaker.Generate();
        // Use same ID but different names
        var customer1 = new Customer(data1.Id, data1.Name);
        var customer2 = new Customer(data1.Id, data2.Name);

        // Act & Assert - entities with same ID are equal regardless of other properties
        customer1.ShouldBe(customer2);
        customer1.GetHashCode().ShouldBe(customer2.GetHashCode());
    }

    [Fact]
    public void Entity_WithBogusData_DifferentIdsNotEqual()
    {
        // Arrange
        var data1 = GuardTestFakers.EntityDataFaker.Generate();
        var data2 = GuardTestFakers.EntityDataFaker.Generate();
        var customer1 = new Customer(data1.Id, data1.Name);
        var customer2 = new Customer(data2.Id, data2.Name);

        // Act & Assert
        if (data1.Id != data2.Id)
        {
            customer1.ShouldNotBe(customer2);
        }
    }

    #endregion

    #region SingleValueObject Guards with Bogus

    private sealed class Money : SingleValueObject<decimal>
    {
        public Money(decimal value) : base(value) { }
    }

    [Fact]
    public void SingleValueObject_WithBogusData_CompareToNull_ReturnsPositive()
    {
        // Arrange
        var data = GuardTestFakers.AmountDataFaker.Generate();
        var money = new Money(data.Value);
        Money? nullMoney = null;

        // Act
        var result = money.CompareTo(nullMoney);

        // Assert
        result.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SingleValueObject_WithBogusData_ImplicitConversion_ReturnsValue()
    {
        // Arrange
        var data = GuardTestFakers.AmountDataFaker.Generate();
        var money = new Money(data.Value);

        // Act
        decimal value = money;

        // Assert
        value.ShouldBe(data.Value);
    }

    [Fact]
    public void SingleValueObject_WithBogusData_SameValueEquals()
    {
        // Arrange
        var data = GuardTestFakers.AmountDataFaker.Generate();
        var money1 = new Money(data.Value);
        var money2 = new Money(data.Value);

        // Act & Assert
        money1.ShouldBe(money2);
        money1.GetHashCode().ShouldBe(money2.GetHashCode());
    }

    [Fact]
    public void SingleValueObject_WithBogusData_DifferentValuesNotEqual()
    {
        // Arrange
        var amounts = GuardTestFakers.AmountDataFaker.Generate(2);
        var money1 = new Money(amounts[0].Value);
        var money2 = new Money(amounts[1].Value);

        // Act & Assert
        if (amounts[0].Value != amounts[1].Value)
        {
            money1.ShouldNotBe(money2);
        }
    }

    #endregion

    #region BusinessRule Guards with Bogus

    private sealed class DynamicBusinessRule : BusinessRule
    {
        private readonly string _errorCode;
        private readonly string _errorMessage;
        private readonly bool _isSatisfied;

        public DynamicBusinessRule(string errorCode, string errorMessage, bool isSatisfied)
        {
            _errorCode = errorCode;
            _errorMessage = errorMessage;
            _isSatisfied = isSatisfied;
        }

        public override string ErrorCode => _errorCode;
        public override string ErrorMessage => _errorMessage;
        public override bool IsSatisfied() => _isSatisfied;
    }

    [Fact]
    public void BusinessRule_WithBogusData_CheckReturnsCorrectError()
    {
        // Arrange
        var data = GuardTestFakers.BusinessRuleDataFaker.Generate();
        var rule = new DynamicBusinessRule(data.ErrorCode, data.ErrorMessage, isSatisfied: false);

        // Act
        var result = rule.Check();

        // Assert
        var error = result.ShouldBeError();
        error.ErrorCode.ShouldBe(data.ErrorCode);
        error.ErrorMessage.ShouldBe(data.ErrorMessage);
    }

    [Fact]
    public void BusinessRule_WithBogusData_SatisfiedReturnsSuccess()
    {
        // Arrange
        var data = GuardTestFakers.BusinessRuleDataFaker.Generate();
        var rule = new DynamicBusinessRule(data.ErrorCode, data.ErrorMessage, isSatisfied: true);

        // Act
        var result = rule.Check();

        // Assert
        result.ShouldBeSuccess();
    }

    [Fact]
    public void BusinessRule_WithBogusData_ThrowIfNotSatisfiedThrows()
    {
        // Arrange
        var data = GuardTestFakers.BusinessRuleDataFaker.Generate();
        var rule = new DynamicBusinessRule(data.ErrorCode, data.ErrorMessage, isSatisfied: false);

        // Act & Assert
        var exception = Should.Throw<BusinessRuleViolationException>(() => rule.ThrowIfNotSatisfied());
        exception.ErrorCode.ShouldBe(data.ErrorCode);
        exception.Message.ShouldBe(data.ErrorMessage);
    }

    #endregion
}
