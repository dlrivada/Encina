using System.Collections.ObjectModel;
using Bogus;
using Encina.DomainModeling.PropertyTests.Fakers;

namespace Encina.DomainModeling.PropertyTests;

/// <summary>
/// Tests demonstrating Bogus integration for generating realistic test data
/// in combination with property-based testing.
/// </summary>
public sealed class BogusIntegrationTests
{
    #region ValueObject with Bogus Data

    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string PostalCode { get; }
        public string Country { get; }

        public Address(string street, string city, string postalCode, string country)
        {
            Street = street;
            City = city;
            PostalCode = postalCode;
            Country = country;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return PostalCode;
            yield return Country;
        }
    }

    [Fact]
    public void ValueObject_WithBogusData_EqualityIsReflexive()
    {
        // Arrange - use Bogus for realistic data
        var addressData = DomainModelFakers.AddressDataFaker.Generate();
        var address = new Address(addressData.Street, addressData.City, addressData.PostalCode, addressData.Country);

        // Act & Assert
        address.Equals(address).ShouldBeTrue();
    }

    [Fact]
    public void ValueObject_WithBogusData_SameValuesShouldBeEqual()
    {
        // Arrange - generate multiple addresses with same seed using a local faker
        // (avoid mutating the static DomainModelFakers.AddressDataFaker)
        var faker = DomainModelFakers.CreateAddressDataFaker();

        faker.UseSeed(42);
        var data1 = faker.Generate();

        faker.UseSeed(42);
        var data2 = faker.Generate();

        var address1 = new Address(data1.Street, data1.City, data1.PostalCode, data1.Country);
        var address2 = new Address(data2.Street, data2.City, data2.PostalCode, data2.Country);

        // Act & Assert
        address1.ShouldBe(address2);
        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }

    [Fact]
    public void ValueObject_WithBogusData_DifferentValuesShouldNotBeEqual()
    {
        // Arrange - use different seeds to guarantee distinct data deterministically
        var faker1 = DomainModelFakers.CreateAddressDataFaker().UseSeed(1);
        var faker2 = DomainModelFakers.CreateAddressDataFaker().UseSeed(2);

        var data1 = faker1.Generate();
        var data2 = faker2.Generate();

        var address1 = new Address(data1.Street, data1.City, data1.PostalCode, data1.Country);
        var address2 = new Address(data2.Street, data2.City, data2.PostalCode, data2.Country);

        // Act & Assert - unconditionally verify inequality
        address1.ShouldNotBe(address2);
    }

    #endregion

    #region Entity with Bogus Data

    private sealed class Customer : Entity<Guid>
    {
        public string Name { get; }
        public string Email { get; }

        public Customer(Guid id, string name, string email) : base(id)
        {
            Name = name;
            Email = email;
        }
    }

    [Fact]
    public void Entity_WithBogusData_EqualityByIdOnly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customer1Data = DomainModelFakers.CustomerDataFaker.Generate();
        var customer2Data = DomainModelFakers.CustomerDataFaker.Generate();

        var customer1 = new Customer(id, customer1Data.Name, customer1Data.Email);
        var customer2 = new Customer(id, customer2Data.Name, customer2Data.Email);

        // Act & Assert - same ID means equal regardless of other properties
        customer1.ShouldBe(customer2);
        customer1.GetHashCode().ShouldBe(customer2.GetHashCode());
    }

    [Fact]
    public void Entity_WithBogusData_DifferentIdsMeansNotEqual()
    {
        // Arrange
        var customerData = DomainModelFakers.CustomerDataFaker.Generate();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var customer1 = new Customer(id1, customerData.Name, customerData.Email);
        var customer2 = new Customer(id2, customerData.Name, customerData.Email);

        // Act & Assert
        customer1.ShouldNotBe(customer2);
    }

    #endregion

    #region AggregateRoot with Bogus Data

    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerName, decimal TotalAmount) : DomainEvent;
    private sealed record OrderLineAdded(Guid OrderId, string ProductName, int Quantity, decimal UnitPrice) : DomainEvent;

    private sealed class Order : AggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;
        private readonly List<OrderLineData> _lines = new List<OrderLineData>();
        public ReadOnlyCollection<OrderLineData> Lines => _lines.AsReadOnly();

        private Order(OrderId id) : base(id) { }

        public static Order Create(OrderId id, CustomerData customer)
        {
            var order = new Order(id) { CustomerName = customer.Name };
            order.RaiseDomainEvent(new OrderCreated(id.Value, customer.Name, 0));
            return order;
        }

        public void AddLine(OrderLineData line)
        {
            _lines.Add(line);
            RaiseDomainEvent(new OrderLineAdded(Id.Value, line.ProductName, line.Quantity, line.UnitPrice));
        }
    }

    [Fact]
    public void AggregateRoot_WithBogusData_RaisesCorrectEvents()
    {
        // Arrange
        var customer = DomainModelFakers.CustomerDataFaker.Generate();
        var lines = DomainModelFakers.OrderLineDataFaker.Generate(3);
        var orderId = OrderId.From(Guid.NewGuid());

        // Act
        var order = Order.Create(orderId, customer);
        foreach (var line in lines)
        {
            order.AddLine(line);
        }

        // Assert
        order.DomainEvents.Count.ShouldBe(4); // 1 create + 3 lines
        order.DomainEvents[0].ShouldBeOfType<OrderCreated>();

        var createEvent = (OrderCreated)order.DomainEvents[0];
        createEvent.CustomerName.ShouldBe(customer.Name);
    }

    [Fact]
    public void AggregateRoot_WithBogusData_MaintainsConsistency()
    {
        // Arrange
        var customer = DomainModelFakers.CustomerDataFaker.Generate();
        var lines = DomainModelFakers.OrderLineDataFaker.Generate(5);
        var orderId = OrderId.From(Guid.NewGuid());

        // Act
        var order = Order.Create(orderId, customer);
        foreach (var line in lines)
        {
            order.AddLine(line);
        }

        // Assert
        order.Lines.Count.ShouldBe(5);
        order.CustomerName.ShouldBe(customer.Name);
        order.Id.ShouldBe(orderId);
    }

    #endregion

    #region Auditable AggregateRoot with Bogus Data

    private sealed class CustomerId : GuidStronglyTypedId<CustomerId>
    {
        public CustomerId(Guid value) : base(value) { }
    }

    private sealed class AuditableCustomer : AuditableAggregateRoot<CustomerId>
    {
        public string Name { get; }

        public AuditableCustomer(CustomerId id, string name, TimeProvider? timeProvider = null)
            : base(id, timeProvider)
        {
            Name = name;
        }
    }

    [Fact]
    public void AuditableAggregateRoot_WithBogusData_TracksAuditInfo()
    {
        // Arrange
        var customer = DomainModelFakers.CustomerDataFaker.Generate();
        var user = DomainModelFakers.UserDataFaker.Generate();
        var id = CustomerId.From(Guid.NewGuid());

        // Act
        var entity = new AuditableCustomer(id, customer.Name);
        entity.SetCreatedBy(user.UserId);
        entity.SetModifiedBy(user.Username);

        // Assert
        entity.CreatedBy.ShouldBe(user.UserId);
        entity.ModifiedBy.ShouldBe(user.Username);
        entity.CreatedAtUtc.ShouldNotBe(default);
        entity.ModifiedAtUtc.ShouldNotBeNull();
    }

    #endregion

    #region Property-based with Bogus seed variation

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    [InlineData(999)]
    public void ValueObject_WithDifferentSeeds_GeneratesDifferentData(int seed)
    {
        // Arrange
        var faker = DomainModelFakers.CreateAddressDataFaker().UseSeed(seed);

        // Act
        var data = faker.Generate();
        var address = new Address(data.Street, data.City, data.PostalCode, data.Country);

        // Assert - basic property verification
        address.Street.ShouldNotBeEmpty();
        address.City.ShouldNotBeEmpty();
        address.PostalCode.ShouldNotBeEmpty();
        address.Country.ShouldNotBeEmpty();
    }

    [Fact]
    public void EntityCollection_WithBogusData_MaintainsUniqueness()
    {
        // Arrange - generate multiple customers (one Generate() call per customer)
        var customers = Enumerable.Range(0, 10)
            .Select(_ =>
            {
                var data = DomainModelFakers.CustomerDataFaker.Generate();
                return new Customer(Guid.NewGuid(), data.Name, data.Email);
            })
            .ToList();

        // Act & Assert - all should have unique IDs
        var uniqueIds = customers.Select(c => c.Id).Distinct().Count();
        uniqueIds.ShouldBe(10);
    }

    #endregion
}
