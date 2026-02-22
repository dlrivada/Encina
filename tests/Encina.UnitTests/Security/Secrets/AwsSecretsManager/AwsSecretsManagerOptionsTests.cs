using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Encina.Security.Secrets.AwsSecretsManager;
using FluentAssertions;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.AwsSecretsManager;

public sealed class AwsSecretsManagerOptionsTests
{
    [Fact]
    public void Region_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.Region.Should().BeNull();
    }

    [Fact]
    public void Credentials_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.Credentials.Should().BeNull();
    }

    [Fact]
    public void ClientConfig_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.ClientConfig.Should().BeNull();
    }

    [Fact]
    public void Region_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();

        options.Region = RegionEndpoint.USEast1;

        options.Region.Should().Be(RegionEndpoint.USEast1);
    }

    [Fact]
    public void Credentials_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();
        var credentials = Substitute.For<AWSCredentials>();

        options.Credentials = credentials;

        options.Credentials.Should().BeSameAs(credentials);
    }

    [Fact]
    public void ClientConfig_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();
        var config = new AmazonSecretsManagerConfig();

        options.ClientConfig = config;

        options.ClientConfig.Should().BeSameAs(config);
    }
}
