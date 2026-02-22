using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.AwsSecretsManager;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.AwsSecretsManager;

public sealed class AwsSecretsManagerServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

        // Pre-register a mock IAmazonSecretsManager so the real factory (which needs AWS
        // credentials) is never invoked. TryAddSingleton in the extension method
        // will skip registration because IAmazonSecretsManager is already present.
        services.AddSingleton(Substitute.For<IAmazonSecretsManager>());

        return services;
    }

    #region Service Registration

    [Fact]
    public void AddAwsSecretsManager_RegistersISecretReader()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretReader>().Should().NotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().Should().NotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().Should().NotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersAwsSecretsManagerProvider()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<AwsSecretsManagerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_WriterAndRotator_ResolveSameProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<AwsSecretsManagerProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.Should().BeSameAs(underlying);
        rotator.Should().BeSameAs(underlying);
    }

    #endregion

    #region Options Configuration

    [Fact]
    public void AddAwsSecretsManager_WithRegion_ConfiguresRegion()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager(aws => aws.Region = RegionEndpoint.USEast1);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>().Value;
        options.Region.Should().Be(RegionEndpoint.USEast1);
    }

    [Fact]
    public void AddAwsSecretsManager_WithCredentials_ConfiguresCredentials()
    {
        var services = CreateServicesWithMockClient();
        var credentials = Substitute.For<AWSCredentials>();

        services.AddAwsSecretsManager(aws => aws.Credentials = credentials);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>().Value;
        options.Credentials.Should().BeSameAs(credentials);
    }

    [Fact]
    public void AddAwsSecretsManager_WithClientConfig_ConfiguresClientConfig()
    {
        var services = CreateServicesWithMockClient();
        var config = new AmazonSecretsManagerConfig();

        services.AddAwsSecretsManager(aws => aws.ClientConfig = config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>().Value;
        options.ClientConfig.Should().BeSameAs(config);
    }

    [Fact]
    public void AddAwsSecretsManager_WithSecretsOptions_ConfiguresSecretsOptions()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager(
            configureSecrets: o =>
            {
                o.EnableCaching = false;
                o.DefaultCacheDuration = TimeSpan.FromMinutes(30);
            });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecretsOptions>>().Value;
        options.EnableCaching.Should().BeFalse();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddAwsSecretsManager_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddAwsSecretsManager();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddAwsSecretsManager_CalledTwice_DoesNotThrow()
    {
        var services = CreateServicesWithMockClient();

        var act = () =>
        {
            services.AddAwsSecretsManager();
            services.AddAwsSecretsManager();
        };

        act.Should().NotThrow();
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddAwsSecretsManager_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddAwsSecretsManager();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    #endregion
}
