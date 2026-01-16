using System.Text.Json;
using Encina.Refit;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Encina.UnitTests.Refit;

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
        using var serviceProvider = services.BuildServiceProvider();

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
        var configureWasCalled = false;
        Uri? capturedBaseAddress = null;
        TimeSpan capturedTimeout = default;

        // Act - Use a callback to verify configuration is applied
        services.AddEncinaRefitClient<ITestApi>(client =>
        {
            client.BaseAddress = baseAddress;
            client.Timeout = TimeSpan.FromSeconds(30);
            configureWasCalled = true;
            capturedBaseAddress = client.BaseAddress;
            capturedTimeout = client.Timeout;
        });
        using var serviceProvider = services.BuildServiceProvider();

        // Trigger the configuration by resolving the Refit client
        var refitClient = serviceProvider.GetRequiredService<ITestApi>();

        // Assert
        refitClient.ShouldNotBeNull();
        configureWasCalled.ShouldBeTrue();
        capturedBaseAddress.ShouldBe(baseAddress);
        capturedTimeout.ShouldBe(TimeSpan.FromSeconds(30));
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
        using var serviceProvider = services.BuildServiceProvider();

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
        using var serviceProvider = services.BuildServiceProvider();

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
        bool hasCustomHeader = false;
        string? customHeaderValue = null;

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("X-Custom-Header", "TestValue");
                hasCustomHeader = client.DefaultRequestHeaders.Contains("X-Custom-Header");
                customHeaderValue = client.DefaultRequestHeaders.GetValues("X-Custom-Header").First();
            })
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();

        // Trigger configuration by resolving the Refit client
        var refitClient = serviceProvider.GetRequiredService<ITestApi>();
        refitClient.ShouldNotBeNull();

        hasCustomHeader.ShouldBeTrue();
        customHeaderValue.ShouldBe("TestValue");
    }

    [Fact]
    public void AddEncinaRefitClient_WithRefitSettings_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var refitSettings = new RefitSettings();
        var expectedBaseAddress = new Uri("https://test.com");
        var configureWasCalled = false;
        Uri? capturedBaseAddress = null;

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>(refitSettings)
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = expectedBaseAddress;
                configureWasCalled = true;
                capturedBaseAddress = client.BaseAddress;
            });

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();

        // Trigger configuration by resolving the Refit client
        var refitClient = serviceProvider.GetRequiredService<ITestApi>();
        refitClient.ShouldNotBeNull();

        configureWasCalled.ShouldBeTrue();
        capturedBaseAddress.ShouldBe(expectedBaseAddress);
    }

    [Fact]
    public void AddEncinaRefitClient_WithSettingsProvider_Chaining_ShouldWork()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedTimeout = TimeSpan.FromSeconds(60);
        var configureWasCalled = false;
        TimeSpan capturedTimeout = default;

        // Act
        var builder = services.AddEncinaRefitClient<ITestApi>(
            sp => new RefitSettings())
            .ConfigureHttpClient(client =>
            {
                client.Timeout = expectedTimeout;
                configureWasCalled = true;
                capturedTimeout = client.Timeout;
            });

        // Assert
        builder.ShouldNotBeNull();
        var serviceProvider = services.BuildServiceProvider();

        // Trigger configuration by resolving the Refit client
        var refitClient = serviceProvider.GetRequiredService<ITestApi>();
        refitClient.ShouldNotBeNull();

        configureWasCalled.ShouldBeTrue();
        capturedTimeout.ShouldBe(expectedTimeout);
    }

    // Test helper
    public interface ITestApi
    {
        [Get("/test")]
        Task<string> GetTestDataAsync();
    }
}
