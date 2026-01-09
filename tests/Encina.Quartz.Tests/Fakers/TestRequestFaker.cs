using Encina.Testing.Bogus;

namespace Encina.Quartz.Tests.Fakers;

/// <summary>
/// Faker for generating test request data used in Quartz job tests.
/// </summary>
public sealed class TestRequestFaker : EncinaFaker<TestRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestRequestFaker"/> class.
    /// </summary>
    public TestRequestFaker()
    {
        CustomInstantiator(f => new TestRequest(f.Lorem.Word()));
    }

    /// <summary>
    /// Configures the faker to generate requests with specific data.
    /// </summary>
    /// <param name="data">The data to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestRequestFaker WithData(string data)
    {
        CustomInstantiator(_ => new TestRequest(data));
        return this;
    }
}

/// <summary>
/// Test request type used in Quartz job tests.
/// </summary>
/// <param name="Data">The test data.</param>
public sealed record TestRequest(string Data) : IRequest<TestResponse>;
