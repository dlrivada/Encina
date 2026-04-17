using Encina.Caching;
using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.Caching;
using Encina.Security.Secrets.GoogleCloudSecretManager;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Security.Secrets.GoogleCloudSecretManager;

public sealed class GoogleCloudSecretManagerServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(NSubstitute.Substitute.For<ICacheProvider>());

        // Pre-register a mock SecretManagerServiceClient so the real factory
        // (which needs GCP credentials) is never invoked. TryAddSingleton
        // in the extension method will skip registration.
        services.AddSingleton(Substitute.For<SecretManagerServiceClient>());

        return services;
    }

    #region Service Registration

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersISecretReader()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretReader>().ShouldNotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().ShouldNotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().ShouldNotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<GoogleCloudSecretManagerProvider>().ShouldNotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersWriterWithCachingDecorator()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<GoogleCloudSecretManagerProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.ShouldBeOfType<CachingSecretWriterDecorator>();
        rotator.ShouldBeSameAs(underlying);
    }

    #endregion

    #region Options Validation

    [Fact]
    public void AddGoogleCloudSecretManager_EmptyProjectId_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddGoogleCloudSecretManager(gcp => { });

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"ProjectId.*required");
    }

    [Fact]
    public void AddGoogleCloudSecretManager_WhitespaceProjectId_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "   ");

        Should.Throw<InvalidOperationException>(act).Message.ShouldMatch(@"ProjectId.*required");
    }

    #endregion

    #region Secrets Options

    [Fact]
    public void AddGoogleCloudSecretManager_WithSecretsOptions_ConfiguresSecretsOptions()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(
            gcp => gcp.ProjectId = "test-project",
            secrets =>
            {
                secrets.EnableCaching = false;
                secrets.DefaultCacheDuration = TimeSpan.FromMinutes(30);
            });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<SecretsOptions>>().Value;
        options.EnableCaching.ShouldBeFalse();
        options.DefaultCacheDuration.ShouldBe(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddGoogleCloudSecretManager_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p");

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddGoogleCloudSecretManager_CalledTwice_DoesNotThrow()
    {
        var services = CreateServicesWithMockClient();

        var act = () =>
        {
            services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p1");
            services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p2");
        };

        Should.NotThrow(act);
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddGoogleCloudSecretManager_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p");

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddGoogleCloudSecretManager_NullConfigureGcp_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddGoogleCloudSecretManager(null!);

        Should.Throw<ArgumentNullException>(act)
            .ParamName.ShouldBe("configureGcp");
    }

    #endregion
}
