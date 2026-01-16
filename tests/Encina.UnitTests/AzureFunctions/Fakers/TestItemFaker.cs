using Encina.Testing.Bogus;

namespace Encina.UnitTests.AzureFunctions.Fakers;

/// <summary>
/// Faker for generating test items used in Fan-Out/Fan-In tests.
/// </summary>
public sealed class TestItemFaker : EncinaFaker<TestItem>
{

    /// <summary>
    /// Initializes a new instance of the <see cref="TestItemFaker"/> class.
    /// </summary>
    public TestItemFaker()
    {
        RuleFor(x => x.Id, (f, _) => f.IndexFaker + 1);
        RuleFor(x => x.Name, f => f.Commerce.ProductName());
    }

    /// <summary>
    /// Configures the faker to generate items with specific ID.
    /// </summary>
    /// <param name="id">The ID to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestItemFaker WithId(int id)
    {
        RuleFor(x => x.Id, _ => id);
        return this;
    }

    /// <summary>
    /// Configures the faker to generate items with specific name.
    /// </summary>
    /// <param name="name">The name to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestItemFaker WithName(string name)
    {
        RuleFor(x => x.Name, _ => name);
        return this;
    }

}

/// <summary>
/// Test item type used in Fan-Out/Fan-In tests.
/// </summary>
public sealed record TestItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
