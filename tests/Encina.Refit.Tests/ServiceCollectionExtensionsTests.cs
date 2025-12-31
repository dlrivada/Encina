using Microsoft.Extensions.DependencyInjection;
using Refit;
using Encina.Refit;
using System.Text.Json;

namespace Encina.Refit.Tests;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEncinaRefitClient_ShouldRegisterRefitClient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRefitClient<ITestApi>();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRefitClient_WithConfigure_ShouldApplyHttpClientConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var baseAddress = new Uri("https://api.test.com");

        // Act
        services.AddEncinaRefitClient<ITestApi>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.BaseAddress.ShouldBe(baseAddress);
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void AddEncinaRefitClient_WithRefitSettings_ShouldRegisterWithSettings()
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
        services.AddEncinaRefitClient<ITestApi>(refitSettings);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRefitClient_WithSettingsProvider_ShouldRegisterWithProvider()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddEncinaRefitClient<ITestApi>(
            sp => new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer()
            });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<ITestApi>();
        client.ShouldNotBeNull();
    }

    [Fact]
    public void AddEncinaRefitClient_ShouldReturnHttpClientBuilder()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>();

        // Assert
        builder.ShouldNotBeNull();
        builder.ShouldBeAssignableTo<IHttpClientBuilder>();
    }

    [Fact]
    public void AddEncinaRefitClient_Chaining_ShouldAllowFurtherConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.DefaultRequestHeaders.Contains("X-Custom-Header").ShouldBeTrue();
        httpClient.DefaultRequestHeaders.GetValues("X-Custom-Header").First().ShouldBe("TestValue");
    }

    [Fact]
    public void AddEncinaRefitClient_WithRefitSettings_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var refitSettings = new RefitSettings();

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>(refitSettings)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://test.com"));

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.BaseAddress.ShouldBe(new Uri("https://test.com"));
    }

    [Fact]
    public void AddEncinaRefitClient_WithSettingsProvider_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>(
            sp => new RefitSettings())
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(60));

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient("ITestApi");
        httpClient.Timeout.ShouldBe(TimeSpan.FromSeconds(60));
    }

    // Test helper
    public interface ITestApi
    {
        [Get("/test")]
        Task<string> GetTestDataAsync();
    }
}
