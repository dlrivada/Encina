using Encina.Testing.Bogus;

namespace Encina.UnitTests.AzureFunctions.Fakers;

/// <summary>
/// Faker for generating test saga data used in Azure Durable Functions tests.
/// </summary>
public sealed class TestSagaDataFaker : EncinaFaker<TestSagaData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestSagaDataFaker"/> class.
    /// </summary>
    public TestSagaDataFaker()
    {
        RuleFor(x => x.Value, f => f.Lorem.Word());
    }

    /// <summary>
    /// Configures the faker to generate saga data with specific value.
    /// </summary>
    /// <param name="value">The value to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestSagaDataFaker WithValue(string value)
    {
        RuleFor(x => x.Value, _ => value);
        return this;
    }
}

/// <summary>
/// Test saga data type used in Durable Functions tests.
/// </summary>
public sealed class TestSagaData
{
    /// <summary>
    /// Gets or sets the saga value.
    /// </summary>
    public string? Value { get; set; }
}
