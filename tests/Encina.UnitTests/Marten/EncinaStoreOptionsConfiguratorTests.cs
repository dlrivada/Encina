using Encina.Marten;
using Encina.Marten.Versioning;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Encina.UnitTests.Marten;

/// <summary>
/// Tests for <see cref="EncinaStoreOptionsConfigurator"/>, the bridge that makes Marten apply the
/// Options-pattern configurators Encina registers (issue #1096).
/// </summary>
public sealed class EncinaStoreOptionsConfiguratorTests
{
    private sealed class RecordingConfigure : IConfigureOptions<StoreOptions>
    {
        public List<string> Log { get; } = [];
        public string Name { get; init; } = "configure";
        public void Configure(StoreOptions options) => Log.Add(Name);
    }

    private sealed class RecordingPostConfigure : IPostConfigureOptions<StoreOptions>
    {
        public List<string> Log { get; } = [];
        public void PostConfigure(string? name, StoreOptions options) => Log.Add("post:" + (name ?? "<null>"));
    }

    [Fact]
    public void Configure_AppliesEveryRegisteredConfigureOptions_InRegistrationOrder()
    {
        var log = new List<string>();
        var first = new RecordingConfigure { Name = "first" };
        var second = new RecordingConfigure { Name = "second" };
        var services = new ServiceCollection();
        services.AddSingleton<IConfigureOptions<StoreOptions>>(first);
        services.AddSingleton<IConfigureOptions<StoreOptions>>(second);
        using var provider = services.BuildServiceProvider();
        var options = new StoreOptions();

        new EncinaStoreOptionsConfigurator().Configure(provider, options);

        log.AddRange(first.Log);
        log.AddRange(second.Log);
        log.ShouldBe(["first", "second"]);
    }

    [Fact]
    public void Configure_AppliesPostConfigureAfterConfigure_WithTheDefaultName()
    {
        var post = new RecordingPostConfigure();
        var services = new ServiceCollection();
        services.AddSingleton<IPostConfigureOptions<StoreOptions>>(post);
        using var provider = services.BuildServiceProvider();

        new EncinaStoreOptionsConfigurator().Configure(provider, new StoreOptions());

        post.Log.ShouldBe(["post:" + Options.DefaultName]);
    }

    [Fact]
    public void Configure_WithNothingRegistered_DoesNotThrow()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();

        Should.NotThrow(() => new EncinaStoreOptionsConfigurator().Configure(provider, new StoreOptions()));
    }

    [Fact]
    public void AddEncinaMartenStoreOptionsBridge_RegistersTheBridgeAsIConfigureMarten_Once()
    {
        var services = new ServiceCollection();

        services.AddEncinaMartenStoreOptionsBridge();
        services.AddEncinaMartenStoreOptionsBridge();

        services.Count(d => d.ServiceType == typeof(IConfigureMarten) && d.ImplementationType == typeof(EncinaStoreOptionsConfigurator)).ShouldBe(1);
    }

    [Fact]
    public void AddEncinaMarten_RegistersTheBridge_AndBothConfiguratorsAsEnumerable()
    {
        var services = new ServiceCollection();

        services.AddEncinaMarten(options =>
        {
            options.EventVersioning.Enabled = true;
            options.Metadata.CorrelationIdEnabled = true;
        });

        services.Count(d => d.ServiceType == typeof(IConfigureMarten) && d.ImplementationType == typeof(EncinaStoreOptionsConfigurator)).ShouldBe(1);
        var implementations = services
            .Where(d => d.ServiceType == typeof(IConfigureOptions<StoreOptions>))
            .Select(d => d.ImplementationType)
            .ToList();
        implementations.ShouldContain(typeof(ConfigureMartenEventVersioning));
        implementations.ShouldContain(typeof(ConfigureMartenEventMetadata));
    }

    [Fact]
    public void Bridge_ThroughMartenIConfigureMarten_RunsEncinaMetadataConfigurator()
    {
        // Same path Marten takes: resolve every IConfigureMarten and apply it to the StoreOptions.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaMarten(options =>
        {
            options.Metadata.CorrelationIdEnabled = true;
            options.Metadata.CausationIdEnabled = true;
        });
        using var provider = services.BuildServiceProvider();
        var storeOptions = new StoreOptions();

        foreach (var configure in provider.GetServices<IConfigureMarten>())
        {
            configure.Configure(provider, storeOptions);
        }

        storeOptions.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeTrue();
        storeOptions.Events.MetadataConfig.CausationIdEnabled.ShouldBeTrue();
    }
}
