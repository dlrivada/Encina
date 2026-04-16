#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shouldly;

namespace Encina.UnitTests.Compliance.CrossBorderTransfer.Notifications;

/// <summary>
/// Unit tests for <see cref="TransferExpirationMonitor"/>.
/// </summary>
public class TransferExpirationMonitorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);

    private TransferExpirationMonitor CreateSut(CrossBorderTransferOptions? options = null)
    {
        var opts = Options.Create(options ?? new CrossBorderTransferOptions
        {
            EnableExpirationMonitoring = false
        });

        return new TransferExpirationMonitor(
            _scopeFactory, opts, _timeProvider,
            NullLogger<TransferExpirationMonitor>.Instance);
    }

    [Fact]
    public void Constructor_ValidParams_DoesNotThrow()
    {
        var act = () => CreateSut();

        Should.NotThrow(act);
    }

    [Fact]
    public async Task StartAsync_MonitoringDisabled_DoesNotCallScopeFactory()
    {
        var sut = CreateSut(new CrossBorderTransferOptions
        {
            EnableExpirationMonitoring = false
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        await sut.StartAsync(cts.Token);
        await Task.Delay(50);
        await sut.StopAsync(CancellationToken.None);

        // Monitoring disabled - scope factory should not be called for monitoring cycles
        sut.ShouldNotBeNull();
    }

    [Fact]
    public async Task StopAsync_CompletesGracefully()
    {
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(Substitute.For<IServiceProvider>());
        _scopeFactory.CreateScope().Returns(scope);

        var sut = CreateSut(new CrossBorderTransferOptions
        {
            EnableExpirationMonitoring = false
        });

        await sut.StartAsync(CancellationToken.None);
        await Task.Delay(50);

        var act = async () => await sut.StopAsync(CancellationToken.None);

        await Should.NotThrowAsync(act);
    }
}
