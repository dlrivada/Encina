using Amazon.KeyManagementService;
using Encina.Messaging.Encryption.AwsKms;
using Encina.Security.Encryption.Abstractions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Encina.UnitTests.Messaging.Encryption.AwsKms;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaMessageEncryptionAwsKms(o => { o.KeyId = "k"; });

        act.Should().Throw<ArgumentNullException>().WithParameterName("services");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddEncinaMessageEncryptionAwsKms(null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("configure");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaMessageEncryptionAwsKms(o => { o.KeyId = "k"; });

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_RegistersAwsKmsOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(o =>
        {
            o.KeyId = "arn:aws:kms:us-east-1:123:key/abc";
            o.Region = "us-east-1";
        });

        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<AwsKmsOptions>>().Value;

        options.KeyId.Should().Be("arn:aws:kms:us-east-1:123:key/abc");
        options.Region.Should().Be("us-east-1");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_RegistersIAmazonKmsService()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(o => { o.KeyId = "k"; });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IAmazonKeyManagementService));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_RegistersIKeyProvider()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(o => { o.KeyId = "k"; });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IKeyProvider));

        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton);
        descriptor.ImplementationType.Should().Be(typeof(AwsKmsKeyProvider));
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_PreRegisteredKmsClient_IsNotReplaced()
    {
        var services = new ServiceCollection();
        var existingClient = Substitute.For<IAmazonKeyManagementService>();

        services.AddSingleton(existingClient);

        services.AddEncinaMessageEncryptionAwsKms(o => { o.KeyId = "k"; });

        var sp = services.BuildServiceProvider();
        var resolved = sp.GetRequiredService<IAmazonKeyManagementService>();

        resolved.Should().BeSameAs(existingClient);
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_WithConfigureEncryption_PassesOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(
            o => { o.KeyId = "k"; },
            e =>
            {
                e.EncryptAllMessages = true;
                e.DefaultKeyId = "custom-key";
            });

        var sp = services.BuildServiceProvider();
        var encOptions = sp.GetRequiredService<IOptions<global::Encina.Messaging.Encryption.MessageEncryptionOptions>>().Value;

        encOptions.EncryptAllMessages.Should().BeTrue();
        encOptions.DefaultKeyId.Should().Be("custom-key");
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_WithRegion_RegistersClientFactory()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(o =>
        {
            o.KeyId = "k";
            o.Region = "eu-west-1";
        });

        // Verify the factory registration exists (resolving would need real AWS credentials)
        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IAmazonKeyManagementService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_WithClientConfig_RegistersClientFactory()
    {
        var services = new ServiceCollection();
        var config = new AmazonKeyManagementServiceConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        services.AddEncinaMessageEncryptionAwsKms(o =>
        {
            o.KeyId = "k";
            o.ClientConfig = config;
        });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IAmazonKeyManagementService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddEncinaMessageEncryptionAwsKms_WithoutRegionOrConfig_RegistersDefaultClientFactory()
    {
        var services = new ServiceCollection();

        services.AddEncinaMessageEncryptionAwsKms(o =>
        {
            o.KeyId = "k";
            // No Region, no ClientConfig
        });

        var descriptor = services.FirstOrDefault(
            d => d.ServiceType == typeof(IAmazonKeyManagementService));

        descriptor.Should().NotBeNull();
        descriptor!.ImplementationFactory.Should().NotBeNull();
    }
}
