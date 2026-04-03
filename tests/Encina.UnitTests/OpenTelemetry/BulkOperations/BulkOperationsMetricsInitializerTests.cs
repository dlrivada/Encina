using Encina.OpenTelemetry.BulkOperations;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.BulkOperations;

/// <summary>
/// Unit tests for <see cref="BulkOperationsMetricsInitializer"/>.
/// </summary>
public sealed class BulkOperationsMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var sut = new BulkOperationsMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var sut = new BulkOperationsMetricsInitializer();
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_WithoutStart_CompletesSuccessfully()
    {
        var sut = new BulkOperationsMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
