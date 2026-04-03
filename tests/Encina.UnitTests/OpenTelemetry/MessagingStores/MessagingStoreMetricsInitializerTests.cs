using Encina.Messaging.Diagnostics;
using Encina.OpenTelemetry.MessagingStores;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.MessagingStores;

/// <summary>
/// Unit tests for <see cref="MessagingStoreMetricsInitializer"/>.
/// </summary>
public sealed class MessagingStoreMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_WithoutCallbacks_CompletesSuccessfully()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var sut = new MessagingStoreMetricsInitializer(services);

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WithCallbacks_CompletesSuccessfully()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton(new MessagingStoreMetricsCallbacks(() => 0L, () => 0L));
        var services = sc.BuildServiceProvider();
        var sut = new MessagingStoreMetricsInitializer(services);

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var sut = new MessagingStoreMetricsInitializer(services);
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
