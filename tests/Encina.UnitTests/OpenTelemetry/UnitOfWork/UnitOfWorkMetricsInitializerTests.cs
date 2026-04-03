using Encina.OpenTelemetry.UnitOfWork;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.UnitOfWork;

/// <summary>
/// Unit tests for <see cref="UnitOfWorkMetricsInitializer"/>.
/// </summary>
public sealed class UnitOfWorkMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var sut = new UnitOfWorkMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var sut = new UnitOfWorkMetricsInitializer();
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
