using Encina.DomainModeling.ContractTests.Fakers;

namespace Encina.DomainModeling.ContractTests;

/// <summary>
/// Enhanced contract tests demonstrating Bogus integration for realistic test data.
/// These tests verify that domain model contracts work correctly with varied input data.
/// </summary>
public sealed class BogusEnhancedContractTests
{
    #region Entity Contracts with Bogus

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
    public void Entity_WithBogusData_IdContract_IsPreserved()
    {
        // Arrange
        var data = ContractTestFakers.CustomerDataFaker.Generate();

        // Act
        var customer = new Customer(data.Id, data.Name, data.Email);

        // Assert - Entity contract: Id must be preserved
        customer.Id.ShouldBe(data.Id);
    }

    [Fact]
    public void Entity_WithBogusData_EqualityContract_SameIdEquals()
    {
        // Arrange
        var data1 = ContractTestFakers.CustomerDataFaker.Generate();
        var data2 = ContractTestFakers.CustomerDataFaker.Generate();

        // Act - Same ID, different other properties
        var customer1 = new Customer(data1.Id, data1.Name, data1.Email);
        var customer2 = new Customer(data1.Id, data2.Name, data2.Email);

        // Assert - Entity contract: equality is based on ID only
        customer1.ShouldBe(customer2);
    }

    [Fact]
    public void Entity_WithBogusData_EqualityAndHashCodeContract_AreConsistent()
    {
        // Arrange
        var data = ContractTestFakers.CustomerDataFaker.Generate();
        var customer1 = new Customer(data.Id, data.Name, data.Email);
        var customer2 = new Customer(data.Id, data.Name, data.Email);

        // Act
        var areEqual = customer1.Equals(customer2);
        var hashCode1 = customer1.GetHashCode();
        var hashCode2 = customer2.GetHashCode();

        // Assert - Equality and HashCode contracts: equal objects have same hash code
        areEqual.ShouldBeTrue();
        hashCode1.ShouldBe(hashCode2);
    }

    #endregion

    #region ValueObject Contracts with Bogus

    private sealed class Email : SingleValueObject<string>
    {
        public Email(string value) : base(value) { }
    }

    [Fact]
    public void ValueObject_WithBogusData_ValueContract_IsPreserved()
    {
        // Arrange
        var data = ContractTestFakers.CustomerDataFaker.Generate();

        // Act
        var email = new Email(data.Email);

        // Assert - ValueObject contract: Value is preserved
        email.Value.ShouldBe(data.Email);
    }

    [Fact]
    public void ValueObject_WithBogusData_ImplicitConversionContract_Works()
    {
        // Arrange
        var data = ContractTestFakers.CustomerDataFaker.Generate();
        var email = new Email(data.Email);

        // Act
        string converted = email;

        // Assert - ValueObject contract: implicit conversion returns value
        converted.ShouldBe(data.Email);
    }

    #endregion

    #region AggregateRoot Contracts with Bogus

    private sealed class OrderId : GuidStronglyTypedId<OrderId>
    {
        public OrderId(Guid value) : base(value) { }
    }

    private sealed record OrderCreated(Guid OrderId, string CustomerName, decimal Amount) : DomainEvent;
    private sealed record OrderUpdated(Guid OrderId, decimal NewAmount) : DomainEvent;

    private sealed class Order : AggregateRoot<OrderId>
    {
        public string CustomerName { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }

        private Order() : base(default!) { }

        public static Order Create(OrderId id, string customerName, decimal totalAmount)
        {
            var order = new Order
            {
                Id = id,
                CustomerName = customerName,
                TotalAmount = totalAmount
            };
            order.RaiseDomainEvent(new OrderCreated(id.Value, customerName, totalAmount));
            return order;
        }

        public void UpdateAmount(decimal newAmount)
        {
            TotalAmount = newAmount;
            RaiseDomainEvent(new OrderUpdated(Id.Value, newAmount));
        }
    }

    [Fact]
    public void AggregateRoot_WithBogusData_DomainEventsContract_RaisedOnCreate()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();
        var orderId = OrderId.From(data.Id);

        // Act
        var order = Order.Create(orderId, data.CustomerName, data.TotalAmount);

        // Assert - AggregateRoot contract: domain event raised on create
        order.DomainEvents.Count.ShouldBe(1);
        order.DomainEvents[0].ShouldBeOfType<OrderCreated>();
    }

    [Fact]
    public void AggregateRoot_WithBogusData_DomainEventsContract_AccumulateOnMutations()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();
        var orderId = OrderId.From(data.Id);
        var order = Order.Create(orderId, data.CustomerName, data.TotalAmount);

        // Act - Multiple mutations
        order.UpdateAmount(data.TotalAmount + 100);
        order.UpdateAmount(data.TotalAmount + 200);

        // Assert - Events accumulate
        order.DomainEvents.Count.ShouldBe(3);
    }

    [Fact]
    public void AggregateRoot_WithBogusData_ClearEventsContract_RemovesAllEvents()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();
        var orderId = OrderId.From(data.Id);
        var order = Order.Create(orderId, data.CustomerName, data.TotalAmount);
        order.UpdateAmount(data.TotalAmount + 100);

        // Act
        order.ClearDomainEvents();

        // Assert - ClearDomainEvents contract: removes all events
        order.DomainEvents.Count.ShouldBe(0);
    }

    #endregion

    #region DomainEvent Contracts with Bogus

    [Fact]
    public void DomainEvent_WithBogusData_OccurredAtContract_IsSet()
    {
        // Arrange
        var data = ContractTestFakers.DomainEventDataFaker.Generate();
        var tolerance = TimeSpan.FromSeconds(2);
        var before = DateTime.UtcNow.Subtract(tolerance);

        // Act
        var domainEvent = new OrderCreated(data.EntityId, "Test Customer", 100m);
        var after = DateTime.UtcNow.Add(tolerance);

        // Assert - DomainEvent contract: OccurredAtUtc is set (with tolerance for load)
        domainEvent.OccurredAtUtc.ShouldBeGreaterThanOrEqualTo(before);
        domainEvent.OccurredAtUtc.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void DomainEvent_EventIdContract_IsUnique()
    {
        // Arrange & Act - use lightweight deterministic construction (no Bogus needed for uniqueness test)
        var events = Enumerable.Range(0, 5)
            .Select(_ => new OrderCreated(Guid.NewGuid(), "Test", 100m))
            .ToList();

        // Assert - DomainEvent contract: EventId is unique
        var uniqueIds = events.Select(e => e.EventId).Distinct().Count();
        uniqueIds.ShouldBe(5);
    }

    #endregion

    #region StronglyTypedId Contracts with Bogus

    [Fact]
    public void StronglyTypedId_WithBogusData_ValueContract_IsPreserved()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();

        // Act
        var orderId = OrderId.From(data.Id);

        // Assert - StronglyTypedId contract: Value is preserved
        orderId.Value.ShouldBe(data.Id);
    }

    [Fact]
    public void StronglyTypedId_WithBogusData_EqualityContract_SameValueEquals()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();

        // Act
        var id1 = OrderId.From(data.Id);
        var id2 = OrderId.From(data.Id);

        // Assert - StronglyTypedId contract: same value means equal
        id1.ShouldBe(id2);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());
    }

    [Fact]
    public void StronglyTypedId_WithBogusData_ImplicitConversionContract_Works()
    {
        // Arrange
        var data = ContractTestFakers.OrderDataFaker.Generate();
        var orderId = OrderId.From(data.Id);

        // Act
        Guid converted = orderId;

        // Assert - StronglyTypedId contract: implicit conversion returns value
        converted.ShouldBe(data.Id);
    }

    #endregion
}
