using System.Text.Json;
using System.Threading.Channels;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Debezium;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Cdc.Debezium;

/// <summary>
/// Registration-level validation for <see cref="ServiceCollectionExtensions.AddEncinaCdcDebezium"/> (#852).
/// </summary>
public sealed class ServiceCollectionExtensionsDebeziumValidationTests
{
    [Fact]
    public void AddEncinaCdcDebezium_InvalidListenUrl_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        Should.Throw<OptionsValidationException>(() =>
            services.AddEncinaCdcDebezium(o => o.ListenUrl = "ftp://+"))
            .Message.ShouldContain("ListenUrl");
    }

    [Fact]
    public void AddEncinaCdcDebezium_RegistersOptionsValidator()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdcDebezium(_ => { });

        services.ShouldContain(d =>
            d.ServiceType == typeof(IValidateOptions<DebeziumCdcOptions>) &&
            d.ImplementationType == typeof(DebeziumCdcOptionsValidator));
    }

    [Fact]
    public void AddEncinaCdcDebezium_DefaultOptions_ProviderBuildsWithValidateOnBuildAndScopes()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICdcPositionStore>());
        services.AddEncinaCdcDebezium(o => o.ChannelCapacity = 25);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        provider.GetRequiredService<DebeziumCdcOptions>().ListenUrl.ShouldBe("http://+");
        provider.GetRequiredService<ICdcConnector>().ShouldNotBeNull();
        provider.GetRequiredService<Channel<JsonElement>>().ShouldNotBeNull();
    }
}
