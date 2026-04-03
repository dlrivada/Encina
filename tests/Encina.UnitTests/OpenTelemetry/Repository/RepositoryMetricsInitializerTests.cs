using Encina.OpenTelemetry.Repository;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.Repository;

/// <summary>
/// Unit tests for <see cref="RepositoryMetricsInitializer"/>.
/// </summary>
public sealed class RepositoryMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var sut = new RepositoryMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var sut = new RepositoryMetricsInitializer();
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
