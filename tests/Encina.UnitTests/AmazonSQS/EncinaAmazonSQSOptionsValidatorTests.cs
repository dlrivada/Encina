using Encina.AmazonSQS;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.AmazonSQS;

/// <summary>
/// Unit tests for <see cref="EncinaAmazonSQSOptionsValidator"/> (#852).
/// </summary>
public sealed class EncinaAmazonSQSOptionsValidatorTests
{
    [Fact]
    public void Validate_NoDefaultQueueUrl_Succeeds()
    {
        new EncinaAmazonSQSOptionsValidator().Validate(null, new EncinaAmazonSQSOptions()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_AwsQueueUrl_Succeeds()
    {
        var options = new EncinaAmazonSQSOptions { DefaultQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/orders" };

        new EncinaAmazonSQSOptionsValidator().Validate(null, options).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("http://sqs.us-east-1.amazonaws.com/123456789012/orders", "HTTPS")]
    [InlineData("https://localhost:4566/000000000000/orders", "loopback")]
    [InlineData("https://169.254.169.254/latest/meta-data", "metadata")]
    [InlineData("orders", "absolute URI")]
    [InlineData("", "must be configured")]
    public void Validate_UnsafeQueueUrl_Fails(string url, string reason)
    {
        var result = new EncinaAmazonSQSOptionsValidator().Validate(null, new EncinaAmazonSQSOptions { DefaultQueueUrl = url });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("EncinaAmazonSQSOptions.DefaultQueueUrl");
        result.FailureMessage.ShouldContain(reason);
    }

    [Fact]
    public void Validate_LocalStackWithOptOuts_SucceedsAndWarnsOnce()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new EncinaAmazonSQSOptionsValidator(factory.CreateLogger<EncinaAmazonSQSOptionsValidator>());
        var options = new EncinaAmazonSQSOptions
        {
            DefaultQueueUrl = "http://localhost:4566/000000000000/orders",
            AllowInsecureHttp = true,
            AllowLocalEndpoints = true,
        };

        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate(null, options).Succeeded.ShouldBeTrue();

        logs.Count(4363, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new EncinaAmazonSQSOptionsValidator().Validate(null, null!));
    }
}
