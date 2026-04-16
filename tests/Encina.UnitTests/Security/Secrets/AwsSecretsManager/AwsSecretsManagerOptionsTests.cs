using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Encina.Security.Secrets.AwsSecretsManager;
using Shouldly;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.AwsSecretsManager;

public sealed class AwsSecretsManagerOptionsTests
{
    [Fact]
    public void Region_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.Region.ShouldBeNull();
    }

    [Fact]
    public void Credentials_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.Credentials.ShouldBeNull();
    }

    [Fact]
    public void ClientConfig_DefaultsToNull()
    {
        var options = new AwsSecretsManagerOptions();

        options.ClientConfig.ShouldBeNull();
    }

    [Fact]
    public void Region_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();

        options.Region = RegionEndpoint.USEast1;

        options.Region.ShouldBe(RegionEndpoint.USEast1);
    }

    [Fact]
    public void Credentials_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();
        var credentials = Substitute.For<AWSCredentials>();

        options.Credentials = credentials;

        options.Credentials.ShouldBeSameAs(credentials);
    }

    [Fact]
    public void ClientConfig_CanBeSet()
    {
        var options = new AwsSecretsManagerOptions();
        var config = new AmazonSecretsManagerConfig();

        options.ClientConfig = config;

        options.ClientConfig.ShouldBeSameAs(config);
    }
}
