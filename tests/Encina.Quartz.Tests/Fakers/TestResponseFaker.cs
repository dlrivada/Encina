using Encina.Testing.Bogus;

namespace Encina.Quartz.Tests.Fakers;

/// <summary>
/// Faker for generating test response data used in Quartz job tests.
/// </summary>
public sealed class TestResponseFaker : EncinaFaker<TestResponse>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResponseFaker"/> class.
    /// </summary>
    public TestResponseFaker()
    {
        CustomInstantiator(f => new TestResponse(f.Lorem.Word()));
    }

    /// <summary>
    /// Configures the faker to generate responses with specific result.
    /// </summary>
    /// <param name="result">The result to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithResult(string result)
    {
        CustomInstantiator(_ => new TestResponse(result));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate success responses.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithSuccess()
    {
        CustomInstantiator(_ => new TestResponse("success"));
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failure responses.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithFailure()
    {
        CustomInstantiator(_ => new TestResponse("failure"));
        return this;
    }
}

/// <summary>
/// Test response type used in Quartz job tests.
/// </summary>
/// <param name="Result">The test result.</param>
public sealed record TestResponse(string Result);
