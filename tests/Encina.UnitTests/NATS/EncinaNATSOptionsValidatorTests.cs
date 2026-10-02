using Encina.NATS;
using Encina.UnitTests.Validation.Endpoints;
using Microsoft.Extensions.Logging;

namespace Encina.UnitTests.NATS;

/// <summary>
/// Unit tests for <see cref="EncinaNATSOptionsValidator"/> (#852).
/// </summary>
public sealed class EncinaNATSOptionsValidatorTests
{
    [Theory]
    [InlineData("nats://nats.example.com:4222")]
    [InlineData("tls://nats.example.com:4222")]
    [InlineData("ws://nats.example.com:8080")]
    [InlineData("wss://nats.example.com:443")]
    [InlineData("nats://user:pass@10.0.0.4:4222")]
    [InlineData("nats://server1.example.com:4222, nats://server2.example.com:4222")]
    public void Validate_RemoteServers_Succeed(string url)
    {
        new EncinaNATSOptionsValidator().Validate(null, new EncinaNATSOptions { Url = url }).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData("nats://localhost:4222", "loopback")]
    [InlineData("nats://nats.example.com:4222,nats://127.0.0.1:4222", "loopback")]
    [InlineData("http://nats.example.com:4222", "allowed schemes")]
    [InlineData("nats.example.com:4222", "allowed schemes")]
    [InlineData("nats://169.254.169.254:4222", "metadata")]
    [InlineData("nats://nats.example.com:4222,", "must be configured")]
    [InlineData("", "must be configured")]
    public void Validate_UnsafeServers_Fail(string url, string reason)
    {
        var result = new EncinaNATSOptionsValidator().Validate(null, new EncinaNATSOptions { Url = url });

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldStartWith("EncinaNATSOptions.Url");
        result.FailureMessage.ShouldContain(reason);
    }

    [Fact]
    public void Validate_NullUrl_Fails()
    {
        new EncinaNATSOptionsValidator().Validate(null, new EncinaNATSOptions { Url = null! }).Failed.ShouldBeTrue();
    }

    [Fact]
    public void Validate_DefaultLocalhostWithOptOut_SucceedsAndWarnsOnce()
    {
        var logs = new CollectingLoggerProvider();
        using var factory = LoggerFactory.Create(b => b.AddProvider(logs));
        var sut = new EncinaNATSOptionsValidator(factory.CreateLogger<EncinaNATSOptionsValidator>());
        var options = new EncinaNATSOptions { AllowLocalEndpoints = true };

        sut.Validate(null, options).Succeeded.ShouldBeTrue();
        sut.Validate(null, options).Succeeded.ShouldBeTrue();

        logs.Count(4209, LogLevel.Warning).ShouldBe(1);
    }

    [Fact]
    public void Validate_NullOptions_Throws()
    {
        Should.Throw<ArgumentNullException>(() => new EncinaNATSOptionsValidator().Validate(null, null!));
    }
}
