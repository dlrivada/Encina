using Encina.Testing.Bogus;

namespace Encina.AwsLambda.Tests.Fakers;

/// <summary>
/// Faker for generating test request data used in AWS Lambda handler tests.
/// </summary>
public sealed class TestRequestFaker : EncinaFaker<TestRequestData>
{

    private int? _id;
    private string? _data;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestRequestFaker"/> class.
    /// </summary>
    public TestRequestFaker()
    {
        CustomInstantiator(f => new TestRequestData(
            _id ?? f.Random.Int(1, 1000),
            _data ?? f.Lorem.Word()));
    }


    /// <summary>
    /// Configures the faker to generate requests with specific ID.
    /// </summary>
    /// <param name="id">The ID to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestRequestFaker WithId(int id)
    {
        _id = id;
        return this;
    }


    /// <summary>
    /// Configures the faker to generate requests with specific data.
    /// </summary>
    /// <param name="data">The data to use.</param>
    /// <returns>This faker instance for method chaining.</returns>
    public TestRequestFaker WithData(string data)
    {
        _data = data;
        return this;
    }
}

/// <summary>
/// Test request data type used in Lambda handler tests.
/// </summary>
/// <param name="Id">The request identifier.</param>
/// <param name="Data">The request data.</param>
public sealed record TestRequestData(int Id, string Data);
