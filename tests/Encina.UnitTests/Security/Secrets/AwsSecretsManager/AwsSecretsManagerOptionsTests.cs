using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Encina.Security.Secrets.AwsSecretsManager;
using NSubstitute;
using Shouldly;

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

    [Fact]
    public void ToString_WhenDefaults_PrintsNull()
    {
        var options = new AwsSecretsManagerOptions();

        var str = options.ToString();

        str.ShouldBe("AwsSecretsManagerOptions { Region=null, Credentials=null }");
    }

    [Fact]
    public void ToString_WhenRegionAndCredentialsSet_MasksCredentials()
    {
        var options = new AwsSecretsManagerOptions
        {
            Region = RegionEndpoint.USEast1,
            Credentials = new BasicAWSCredentials("AKIA1234567890", "super-secret-key")
        };

        var str = options.ToString();

        str.ShouldBe("AwsSecretsManagerOptions { Region=us-east-1, Credentials=*** }");
        str.ShouldNotContain("AKIA1234567890");
        str.ShouldNotContain("super-secret-key");
    }

    [Fact]
    public void JsonSerialize_ExcludesCredentials()
    {
        var options = new AwsSecretsManagerOptions
        {
            Region = RegionEndpoint.USEast1,
            Credentials = new BasicAWSCredentials("AKIA1234567890", "super-secret-key")
        };

        var json = System.Text.Json.JsonSerializer.Serialize(options);

        json.ShouldNotContain("Credentials");
        json.ShouldNotContain("AKIA1234567890");
        json.ShouldNotContain("super-secret-key");
        json.ShouldContain("Region");
    }
}
