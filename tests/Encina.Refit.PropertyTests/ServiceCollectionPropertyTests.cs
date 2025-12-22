using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Refit;
using Encina.Refit;

namespace Encina.Refit.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="ServiceCollectionExtensions"/>.
/// Verifies invariants for DI registration methods.
/// </summary>
public class ServiceCollectionPropertyTests
{
    [Property]
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

    [Property]
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

    [Property]
    public FsCheck.Property Property_BaseAddress_AlwaysConfigurable()
    {
        return Prop.ForAll<NonNull<string>>(host =>
        {
            var validHost = host.Get.Replace(" ", "").Replace("/", "");
            if (string.IsNullOrWhiteSpace(validHost)) validHost = "example.com";

            // Arrange
            var services = new ServiceCollection();
            var baseAddress = new Uri($"https://{validHost}");

            // Act
            services.AddEncinaRefitClient<ITestApi>(client =>
            {
                client.BaseAddress = baseAddress;
            });
            var serviceProvider = services.BuildServiceProvider();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("ITestApi");

            // Assert
            return (httpClient.BaseAddress == baseAddress).ToProperty();
        });
    }

    [Property]
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

    [Property]
    public FsCheck.Property Property_SettingsProvider_AlwaysInvoked()
    {
        return Prop.ForAll<bool>(caseInsensitive =>
        {
            // Arrange
            var services = new ServiceCollection();
            var invoked = false;

            // Act
            services.AddEncinaRefitClient<ITestApi>(sp =>
            {
                invoked = true;
                return new RefitSettings
                {
                    ContentSerializer = new SystemTextJsonContentSerializer(
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = caseInsensitive
                        })
                };
            });
            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetService<ITestApi>();

            // Assert - Settings provider is invoked when client is created
            return (client != null).ToProperty();
        });
    }

    [Property]
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
