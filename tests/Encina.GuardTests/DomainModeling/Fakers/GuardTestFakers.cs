using Bogus;
using Encina.Testing.Bogus;

namespace Encina.GuardTests.DomainModeling.Fakers;

/// <summary>
/// Fakers for generating test data in Guard tests.
/// </summary>
public static class GuardTestFakers
{
    /// <summary>
    /// Faker for generating address data.
    /// </summary>
    public static Faker<AddressData> AddressDataFaker { get; } = new EncinaFaker<AddressData>()
        .RuleFor(a => a.Street, f => f.Address.StreetAddress())
        .RuleFor(a => a.City, f => f.Address.City());

    /// <summary>
    /// Creates a new instance of address data faker for tests that need to mutate faker state (e.g., UseSeed).
    /// </summary>
    /// <returns>A new <see cref="Faker{AddressData}"/> instance with the same configuration as <see cref="AddressDataFaker"/>.</returns>
    public static Faker<AddressData> CreateAddressDataFaker() => new EncinaFaker<AddressData>()
        .RuleFor(a => a.Street, f => f.Address.StreetAddress())
        .RuleFor(a => a.City, f => f.Address.City());

    /// <summary>
    /// Faker for generating entity data.
    /// </summary>
    public static Faker<EntityData> EntityDataFaker { get; } = new EncinaFaker<EntityData>()
        .RuleFor(e => e.Id, f => f.Random.Guid())
        .RuleFor(e => e.Name, f => f.Name.FullName());

    /// <summary>
    /// Faker for generating business rule test data.
    /// </summary>
    public static Faker<BusinessRuleTestData> BusinessRuleDataFaker { get; } = new EncinaFaker<BusinessRuleTestData>()
        .RuleFor(b => b.ErrorCode, f => f.Random.AlphaNumeric(8).ToUpperInvariant())
        .RuleFor(b => b.ErrorMessage, f => f.Lorem.Sentence());

    /// <summary>
    /// Faker for generating decimal amounts.
    /// </summary>
    public static Faker<AmountData> AmountDataFaker { get; } = new EncinaFaker<AmountData>()
        .RuleFor(a => a.Value, f => f.Finance.Amount(0.01m, 10000m));
}

/// <summary>
/// Test data for address.
/// </summary>
public record AddressData
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
}

/// <summary>
/// Test data for entity.
/// </summary>
public record EntityData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Test data for business rule.
/// </summary>
public record BusinessRuleTestData
{
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

/// <summary>
/// Test data for amount.
/// </summary>
public record AmountData
{
    public decimal Value { get; init; }
}
