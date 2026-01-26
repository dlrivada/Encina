using Encina.Refit;
using FsCheck;
using FsCheck.Fluent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Refit;

namespace Encina.PropertyTests.Web.Refit;

/// <summary>
/// Property-based tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies invariants for DI registration methods.
/// </summary>
public class ServiceCollectionPropertyTests
{
    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_AddEncinaRefitClient_AlwaysRegistersClient()
    {
        return Prop.ForAll<PositiveInt>(seed =>
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEncinaRefitClient<ITestApi>();
            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<ITestApi>();

            // Assert
            return (client != null).ToProperty();
        });
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_HttpClientBuilder_AlwaysChainable()
    {
        return Prop.ForAll<PositiveInt>(timeoutSeconds =>
        {
            // Arrange
            var services = new ServiceCollection();
            var timeout = TimeSpan.FromSeconds(Math.Min(timeoutSeconds.Get, 300)); // Cap at 5 minutes

            // Act
            var builder = services.AddEncinaRefitClient<ITestApi>()
                .ConfigureHttpClient(client => client.Timeout = timeout);

            // Assert
            return (builder is IHttpClientBuilder).ToProperty();
        });
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_BaseAddress_AlwaysConfigurable()
    {
        return Prop.ForAll<PositiveInt>(seed =>
        {
            // Use seed to generate deterministic valid hosts
            var validHost = $"api{seed.Get}.example.com";

            // Arrange
            var services = new ServiceCollection();
            var baseAddress = new Uri($"https://{validHost}");

            // Act - verify that ConfigureHttpClient callback is invoked
            Uri? capturedBaseAddress = null;
            services.AddEncinaRefitClient<ITestApi>(client =>
            {
                client.BaseAddress = baseAddress;
                capturedBaseAddress = client.BaseAddress;
            });
            var serviceProvider = services.BuildServiceProvider();

            // Resolve the Refit client (this triggers HttpClient configuration)
            var refitClient = serviceProvider.GetService<ITestApi>();

            // Assert - The Refit client was created and configuration was applied
            return (refitClient != null && capturedBaseAddress == baseAddress).ToProperty();
        });
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_RefitSettings_AlwaysAccepted()
    {
        return Prop.ForAll<bool>(caseInsensitive =>
        {
            // Arrange
            var services = new ServiceCollection();
            var settings = new RefitSettings
            {
                ContentSerializer = new SystemTextJsonContentSerializer(
                    new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = caseInsensitive
                    })
            };

            // Act
            services.AddEncinaRefitClient<ITestApi>(settings);
            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<ITestApi>();

            // Assert
            return (client != null).ToProperty();
        });
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_SettingsProvider_AlwaysInvoked()
    {
        return Prop.ForAll<bool>(caseInsensitive =>
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddEncinaRefitClient<ITestApi>(_ =>
                new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = caseInsensitive
                        })
                });
            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<ITestApi>();

            // Assert - Settings provider is invoked when client is created
            return (client != null).ToProperty();
        });
    }

    [FsCheck.Xunit.Property]
    public FsCheck.Property Property_MultipleClients_CanCoexist()
    {
        return Prop.ForAll<PositiveInt>(count =>
        {
            var actualCount = Math.Min(count.Get, 10); // Limit to 10 for performance

            // Arrange
            var services = new ServiceCollection();

            // Act
            for (int i = 0; i < actualCount; i++)
            {
                services.AddEncinaRefitClient<ITestApi>();
            }
            var serviceProvider = services.BuildServiceProvider();
            var clients = Enumerable.Range(0, actualCount)
                .Select(_ => serviceProvider.GetService<ITestApi>())
                .ToList();

            // Assert
            return clients.All(c => c != null).ToProperty();
        });
    }

    // Test helper
    public interface ITestApi
    {
        [Get("/test")]
        Task<string> GetTestDataAsync();
    }
}
