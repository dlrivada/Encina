using Amazon;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.AwsSecretsManager;
using Encina.Security.Secrets.Caching;
using Shouldly;
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
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());

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
        provider.GetService<ISecretReader>().ShouldNotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().ShouldNotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().ShouldNotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersAwsSecretsManagerProvider()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        provider.GetService<AwsSecretsManagerProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddAwsSecretsManager_RegistersWriterWithCachingDecorator()
    {
        var services = CreateServicesWithMockClient();

        services.AddAwsSecretsManager();

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<AwsSecretsManagerProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        // Writer is wrapped by CachingSecretWriterDecorator when caching is enabled (default).
        // Verify the writer is wrapped by the caching invalidation decorator.
        writer.ShouldBeOfType<CachingSecretWriterDecorator>();
        rotator.ShouldBeSameAs(underlying);
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
        options.Region.ShouldBe(RegionEndpoint.USEast1);
    }

    [Fact]
    public void AddAwsSecretsManager_WithCredentials_ConfiguresCredentials()
    {
        var services = CreateServicesWithMockClient();
        var credentials = Substitute.For<AWSCredentials>();

        services.AddAwsSecretsManager(aws => aws.Credentials = credentials);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>().Value;
        options.Credentials.ShouldBeSameAs(credentials);
    }

    [Fact]
    public void AddAwsSecretsManager_WithClientConfig_ConfiguresClientConfig()
    {
        var services = CreateServicesWithMockClient();
        var config = new AmazonSecretsManagerConfig();

        services.AddAwsSecretsManager(aws => aws.ClientConfig = config);

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AwsSecretsManagerOptions>>().Value;
        options.ClientConfig.ShouldBeSameAs(config);
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
        options.EnableCaching.ShouldBeFalse();
        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddAwsSecretsManager_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddAwsSecretsManager();

        result.ShouldBeSameAs(services);
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

        Should.NotThrow(act);
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddAwsSecretsManager_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddAwsSecretsManager();

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    #endregion
}
