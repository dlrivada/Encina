using Encina.OpenTelemetry.Audit;
using Shouldly;

namespace Encina.UnitTests.OpenTelemetry.Audit;

/// <summary>
/// Unit tests for <see cref="AuditMetricsInitializer"/>.
/// </summary>
public sealed class AuditMetricsInitializerTests
{
    [Fact]
    public async Task StartAsync_CompletesSuccessfully()
    {
        var sut = new AuditMetricsInitializer();

        await Should.NotThrowAsync(() => sut.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StopAsync_CompletesSuccessfully()
    {
        var sut = new AuditMetricsInitializer();
        await sut.StartAsync(CancellationToken.None);

        await Should.NotThrowAsync(() => sut.StopAsync(CancellationToken.None));
    }
}
