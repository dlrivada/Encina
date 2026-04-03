using Encina.OpenTelemetry.QueryCache;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.QueryCache;

/// <summary>
/// Unit tests for <see cref="QueryCacheMetricsInitializer"/>.
/// </summary>
public sealed class QueryCacheMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var sut = new QueryCacheMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var sut = new QueryCacheMetricsInitializer();
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
