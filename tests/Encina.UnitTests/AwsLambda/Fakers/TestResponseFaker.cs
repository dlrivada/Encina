using Encina.Testing.Bogus;

namespace Encina.UnitTests.AwsLambda.Fakers;

/// <summary>
/// Faker for generating test response data used in AWS Lambda handler tests.
/// </summary>
public sealed class TestResponseFaker : EncinaFaker<TestResponseData>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResponseFaker"/> class.
    /// </summary>
    public TestResponseFaker()
    {
        CustomInstantiator(f => new TestResponseData(
            f.Lorem.Sentence(),
            f.Random.Bool()));

        // Removed ineffective RuleFor lines for init-only properties
    }

    /// <summary>
    /// Configures the faker to generate successful responses.
    /// </summary>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithSuccess()
    {
        RuleFor(x => x.Success, _ => true);
        RuleFor(x => x.Message, f => f.Lorem.Sentence());
        return this;
    }

    /// <summary>
    /// Configures the faker to generate failure responses.
    /// </summary>
    /// <param name="errorMessage">Optional error message.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithFailure(string? errorMessage = null)
    {
        RuleFor(x => x.Success, _ => false);
        RuleFor(x => x.Message, f => errorMessage ?? f.Lorem.Sentence());
        return this;
    }

    /// <summary>
    /// Configures the faker to generate responses with specific message.
    /// </summary>
    /// <param name="message">The message to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestResponseFaker WithMessage(string message)
    {
        RuleFor(x => x.Message, f => message);
        return this;
    }
}

/// <summary>
/// Test response data type used in Lambda handler tests.
/// </summary>
/// <param name="Message">The response message.</param>
/// <param name="Success">Indicates whether the operation was successful.</param>
public sealed record TestResponseData(string Message, bool Success);
