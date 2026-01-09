using Encina.Testing.Bogus;

namespace Encina.Hangfire.Tests.Fakers;

/// <summary>
/// Faker for generating test request data used in Hangfire job adapter tests.
/// </summary>
public sealed class TestRequestFaker : EncinaFaker<TestRequestData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestRequestFaker"/> class.
    /// </summary>
    public TestRequestFaker()
    {
        CustomInstantiator(f => new TestRequestData(f.Lorem.Word()));
    }

    /// <summary>
    /// Configures the faker to generate requests with specific data.
    /// </summary>
    /// <param name="data">The data to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestRequestFaker WithData(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CustomInstantiator(_ => new TestRequestData(data));
        return this;
    }
}

/// <summary>
/// Test request data type used in Hangfire job adapter tests.
/// </summary>
public sealed record TestRequestData
{
    public string Data { get; init; }

    public TestRequestData(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }
}
