using Bogus;

namespace Encina.DomainModeling.PropertyTests.Fakers;

/// <summary>
/// Fakers for generating realistic test data in DomainModeling property tests.
/// </summary>
public static class DomainModelFakers
{
    /// <summary>
    /// Creates a faker for generating customer names.
    /// </summary>
    public static Faker<CustomerData> CustomerDataFaker { get; } = new EncinaFaker<CustomerData>()
        .RuleFor(c => c.Name, f => f.Person.FullName)
        .RuleFor(c => c.Email, f => f.Internet.Email());

    /// <summary>
    /// Creates a faker for generating product data.
    /// </summary>
    public static Faker<ProductData> ProductDataFaker { get; } = new EncinaFaker<ProductData>()
        .RuleFor(p => p.Name, f => f.Commerce.ProductName())
        .RuleFor(p => p.Price, f => f.Finance.Amount(1, 1000));

    /// <summary>
    /// Creates a faker for generating address data.
    /// </summary>
    public static Faker<AddressData> AddressDataFaker { get; } = new EncinaFaker<AddressData>()
        .RuleFor(a => a.Street, f => f.Address.StreetAddress())
        .RuleFor(a => a.City, f => f.Address.City())
        .RuleFor(a => a.PostalCode, f => f.Address.ZipCode())
        .RuleFor(a => a.Country, f => f.Address.Country());

    /// <summary>
    /// Creates a new instance of address data faker for tests that need to mutate faker state (e.g., UseSeed).
    /// </summary>
    /// <returns>A new <see cref="Faker{AddressData}"/> instance with the same configuration as <see cref="AddressDataFaker"/>.</returns>
    public static Faker<AddressData> CreateAddressDataFaker() => new EncinaFaker<AddressData>()
        .RuleFor(a => a.Street, f => f.Address.StreetAddress())
        .RuleFor(a => a.City, f => f.Address.City())
        .RuleFor(a => a.PostalCode, f => f.Address.ZipCode())
        .RuleFor(a => a.Country, f => f.Address.Country());

    /// <summary>
    /// Creates a faker for generating order line data.
    /// </summary>
    public static Faker<OrderLineData> OrderLineDataFaker { get; } = new EncinaFaker<OrderLineData>()
        .RuleFor(l => l.ProductName, f => f.Commerce.ProductName())
        .RuleFor(l => l.Quantity, f => f.Random.QuantityValue(1, 100))
        .RuleFor(l => l.UnitPrice, f => f.Finance.Amount(1, 500));

    /// <summary>
    /// Creates a faker for generating user identifiers.
    /// </summary>
    public static Faker<UserData> UserDataFaker { get; } = new EncinaFaker<UserData>()
        .RuleFor(u => u.UserId, f => f.Random.UserId())
        .RuleFor(u => u.Username, f => f.Internet.UserName())
        .RuleFor(u => u.TenantId, f => f.Random.TenantId());
}

/// <summary>
/// Test data class for customer information.
/// </summary>
public class CustomerData
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Test data class for product information.
/// </summary>
public class ProductData
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

/// <summary>
/// Test data class for address information.
/// </summary>
public class AddressData
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Test data class for order line information.
/// </summary>
public class OrderLineData
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

/// <summary>
/// Test data class for user information.
/// </summary>
public class UserData
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
}
