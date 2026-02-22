using Encina.Security.Secrets;
using Encina.Security.Secrets.Abstractions;
using Encina.Security.Secrets.GoogleCloudSecretManager;
using FluentAssertions;
using Google.Cloud.SecretManager.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Encina.UnitTests.Security.Secrets.GoogleCloudSecretManager;

public sealed class GoogleCloudSecretManagerServiceCollectionExtensionsTests
{
    private static ServiceCollection CreateServicesWithMockClient()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();

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
        provider.GetService<ISecretReader>().Should().NotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersISecretWriter()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretWriter>().Should().NotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersISecretRotator()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<ISecretRotator>().Should().NotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_RegistersProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        provider.GetService<GoogleCloudSecretManagerProvider>().Should().NotBeNull();
    }

    [Fact]
    public void AddGoogleCloudSecretManager_WriterAndRotator_ResolveSameProviderInstance()
    {
        var services = CreateServicesWithMockClient();

        services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "test-project");

        var provider = services.BuildServiceProvider();
        var underlying = provider.GetRequiredService<GoogleCloudSecretManagerProvider>();
        var writer = provider.GetRequiredService<ISecretWriter>();
        var rotator = provider.GetRequiredService<ISecretRotator>();

        writer.Should().BeSameAs(underlying);
        rotator.Should().BeSameAs(underlying);
    }

    #endregion

    #region Options Validation

    [Fact]
    public void AddGoogleCloudSecretManager_EmptyProjectId_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddGoogleCloudSecretManager(gcp => { });

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProjectId*required*");
    }

    [Fact]
    public void AddGoogleCloudSecretManager_WhitespaceProjectId_ThrowsInvalidOperationException()
    {
        var services = CreateServicesWithMockClient();

        var act = () => services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "   ");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ProjectId*required*");
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
        options.EnableCaching.Should().BeFalse();
        options.DefaultCacheDuration.Should().Be(TimeSpan.FromMinutes(30));
    }

    #endregion

    #region Chaining and Idempotency

    [Fact]
    public void AddGoogleCloudSecretManager_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p");

        result.Should().BeSameAs(services);
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

        act.Should().NotThrow();
    }

    #endregion

    #region Null Guards

    [Fact]
    public void AddGoogleCloudSecretManager_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddGoogleCloudSecretManager(gcp => gcp.ProjectId = "p");

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddGoogleCloudSecretManager_NullConfigureGcp_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        var act = () => services.AddGoogleCloudSecretManager(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("configureGcp");
    }

    #endregion
}
