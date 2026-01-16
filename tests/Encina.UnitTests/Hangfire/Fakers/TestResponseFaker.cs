using Encina.Testing.Bogus;

namespace Encina.UnitTests.Hangfire.Fakers;

/// <summary>
/// Faker for generating test response data used in Hangfire job adapter tests.
/// </summary>
public sealed class TestResponseFaker : EncinaFaker<TestResponseData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResponseFaker"/> class.
    /// </summary>
    public TestResponseFaker()
    {
        CustomInstantiator(f => new TestResponseData(f.Lorem.Word()));
    }

    /// <summary>
    /// Configures the faker to generate responses with specific result.
    /// </summary>
    /// <param name="result">The result to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithResult(string result)
    {
        ArgumentNullException.ThrowIfNull(result);
        CustomInstantiator(_ => new TestResponseData(result));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate success responses.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithSuccess()
    {
        return WithResult("success");
    }

    /// <summary>
    /// Configures the faker to generate failure responses.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithFailure()
    {
        return WithResult("failure");
    }
}

/// <summary>
/// Test response data type used in Hangfire job adapter tests.
/// </summary>
/// <param name="Result">The test result.</param>
public sealed record TestResponseData(string Result);
