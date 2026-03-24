using Amazon.KeyManagementService;
using Encina.Messaging.Encryption.AwsKms;
using FluentAssertions;

namespace Encina.UnitTests.Messaging.Encryption.AwsKms;

public class AwsKmsOptionsTests
{
    [Fact]
    public void Defaults_KeyId_IsNull()
    {
        var options = new AwsKmsOptions();
        options.KeyId.Should().BeNull();
    }

    [Fact]
    public void Defaults_EncryptionAlgorithm_IsSymmetricDefault()
    {
        var options = new AwsKmsOptions();
        options.EncryptionAlgorithm.Should().Be("SYMMETRIC_DEFAULT");
    }

    [Fact]
    public void Defaults_Region_IsNull()
    {
        var options = new AwsKmsOptions();
        options.Region.Should().BeNull();
    }

    [Fact]
    public void Defaults_ClientConfig_IsNull()
    {
        var options = new AwsKmsOptions();
        options.ClientConfig.Should().BeNull();
    }

    [Fact]
    public void KeyId_IsSettable()
    {
        var options = new AwsKmsOptions { KeyId = "arn:aws:kms:us-east-1:123456:key/abc-def" };
        options.KeyId.Should().Be("arn:aws:kms:us-east-1:123456:key/abc-def");
    }

    [Fact]
    public void EncryptionAlgorithm_IsSettable()
    {
        var options = new AwsKmsOptions { EncryptionAlgorithm = "RSAES_OAEP_SHA_256" };
        options.EncryptionAlgorithm.Should().Be("RSAES_OAEP_SHA_256");
    }

    [Fact]
    public void Region_IsSettable()
    {
        var options = new AwsKmsOptions { Region = "eu-west-1" };
        options.Region.Should().Be("eu-west-1");
    }

    [Fact]
    public void ClientConfig_IsSettable()
    {
        var config = new AmazonKeyManagementServiceConfig();
        var options = new AwsKmsOptions { ClientConfig = config };
        options.ClientConfig.Should().BeSameAs(config);
    }

    [Fact]
    public void AllProperties_AreSettable()
    {
        var config = new AmazonKeyManagementServiceConfig();

        var options = new AwsKmsOptions
        {
            KeyId = "alias/my-key",
            EncryptionAlgorithm = "RSAES_OAEP_SHA_1",
            Region = "us-west-2",
            ClientConfig = config
        };

        options.KeyId.Should().Be("alias/my-key");
        options.EncryptionAlgorithm.Should().Be("RSAES_OAEP_SHA_1");
        options.Region.Should().Be("us-west-2");
        options.ClientConfig.Should().BeSameAs(config);
    }
}
