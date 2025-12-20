using Microsoft.Extensions.DependencyInjection;
using Refit;
using SimpleMediator.Refit;
using System.Text.Json;

namespace SimpleMediator.Refit.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSimpleMediatorRefitClient_ShouldRegisterRefitClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorRefitClient<ITestApi>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_WithConfigure_ShouldApplyHttpClientConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseAddress = new Uri("https://api.test.com");

        // Act
        services.AddSimpleMediatorRefitClient<ITestApi>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.BaseAddress.Should().Be(baseAddress);
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_WithRefitSettings_ShouldRegisterWithSettings()
    {
        // Arrange
        var services = new ServiceCollection();
        var refitSettings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
        };

        // Act
        services.AddSimpleMediatorRefitClient<ITestApi>(refitSettings);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_WithSettingsProvider_ShouldRegisterWithProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddSimpleMediatorRefitClient<ITestApi>(
            sp => new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer()
            });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.Should().NotBeNull();
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_ShouldReturnHttpClientBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddSimpleMediatorRefitClient<ITestApi>();

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeAssignableTo<IHttpClientBuilder>();
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_Chaining_ShouldAllowFurtherConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddSimpleMediatorRefitClient<ITestApi>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Assert
        builder.Should().NotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.DefaultRequestHeaders.Contains("X-Custom-Header").Should().BeTrue();
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_WithRefitSettings_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var refitSettings = new RefitSettings();

        // Act
        var builder = services.AddSimpleMediatorRefitClient<ITestApi>(refitSettings)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://test.com"));

        // Assert
        builder.Should().NotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.BaseAddress.Should().Be(new Uri("https://test.com"));
    }

    [Fact]
    public void AddSimpleMediatorRefitClient_WithSettingsProvider_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddSimpleMediatorRefitClient<ITestApi>(
            sp => new RefitSettings())
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(60));

        // Assert
        builder.Should().NotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(60));
    }

    // Test helper
    public interface ITestApi
    {
        [Get("/test")]
        Task<string> GetTestDataAsync();
    }
}
