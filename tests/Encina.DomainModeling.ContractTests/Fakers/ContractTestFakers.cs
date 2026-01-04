using Bogus;

namespace Encina.DomainModeling.ContractTests.Fakers;

/// <summary>
/// Fakers for generating test data in Contract tests.
/// </summary>
public static class ContractTestFakers
{
    /// <summary>
    /// Faker for generating customer data.
    /// </summary>
    public static Faker<CustomerData> CustomerDataFaker { get; } = new EncinaFaker<CustomerData>()
        .RuleFor(c => c.Id, f => f.Random.Guid())
        .RuleFor(c => c.Name, f => f.Person.FullName)
        .RuleFor(c => c.Email, f => f.Internet.Email());

    /// <summary>
    /// Faker for generating order data.
    /// </summary>
    public static Faker<OrderData> OrderDataFaker { get; } = new EncinaFaker<OrderData>()
        .RuleFor(o => o.Id, f => f.Random.Guid())
        .RuleFor(o => o.CustomerName, f => f.Person.FullName)
        .RuleFor(o => o.TotalAmount, f => f.Finance.Amount(10, 5000));

    /// <summary>
    /// Faker for generating domain event data.
    /// </summary>
    public static Faker<DomainEventData> DomainEventDataFaker { get; } = new EncinaFaker<DomainEventData>()
        .RuleFor(d => d.EventId, f => f.Random.Guid())
        .RuleFor(d => d.EntityId, f => f.Random.Guid())
        .RuleFor(d => d.EventName, f => f.NotificationType());
}

/// <summary>
/// Customer test data.
/// </summary>
public class CustomerData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Order test data.
/// </summary>
public class OrderData
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Domain event test data.
/// </summary>
public class DomainEventData
{
    public Guid EventId { get; set; }
    public Guid EntityId { get; set; }
    public string EventName { get; set; } = string.Empty;
}
