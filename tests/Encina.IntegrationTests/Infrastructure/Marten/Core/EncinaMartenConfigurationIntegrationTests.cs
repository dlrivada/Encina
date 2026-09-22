using Encina.IntegrationTests.Infrastructure.Marten.Fixtures;
using Encina.Marten;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.IntegrationTests.Infrastructure.Marten.Core;

/// <summary>
/// Proves that a document store built through DI (<c>AddMarten</c> + <c>AddEncinaMarten</c>) carries the
/// configuration Encina registers through <c>IConfigureOptions&lt;StoreOptions&gt;</c> (issue #1096).
/// </summary>
[Collection(MartenCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "PostgreSQL")]
public sealed class EncinaMartenConfigurationIntegrationTests : IAsyncLifetime
{
    private readonly MartenFixture _fixture;

    public EncinaMartenConfigurationIntegrationTests(MartenFixture fixture)
    {
        _fixture = fixture;
    }

    public ValueTask InitializeAsync()
    {
        Assert.SkipUnless(_fixture.IsAvailable, "Marten PostgreSQL container not available");
        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task DocumentStore_BuiltThroughDI_HasEncinaMetadataColumnsEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarten(_fixture.ConnectionString);
        services.AddEncinaMarten(options =>
        {
            options.Metadata.CorrelationIdEnabled = true;
            options.Metadata.CausationIdEnabled = true;
            options.Metadata.HeadersEnabled = true;
        });

        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IDocumentStore>();

        store.Options.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeTrue();
        store.Options.Events.MetadataConfig.CausationIdEnabled.ShouldBeTrue();
        store.Options.Events.MetadataConfig.HeadersEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task DocumentStore_BuiltThroughDI_WithoutBridge_IgnoresEncinaConfigurators()
    {
        // Documents the defect the bridge fixes: Marten alone never applies IConfigureOptions<StoreOptions>.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMarten(_fixture.ConnectionString);
        services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<StoreOptions>>(
            new EnableCorrelation());

        await using var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IDocumentStore>();

        store.Options.Events.MetadataConfig.CorrelationIdEnabled.ShouldBeFalse();
    }

    private sealed class EnableCorrelation : Microsoft.Extensions.Options.IConfigureOptions<StoreOptions>
    {
        public void Configure(StoreOptions options) => options.Events.MetadataConfig.CorrelationIdEnabled = true;
    }
}
