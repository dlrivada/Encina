using Encina.Compliance.CrossBorderTransfer;
using Encina.Compliance.CrossBorderTransfer.Notifications;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="TransferExpirationMonitor"/> verifying constructor null checks.
/// </summary>
public class TransferExpirationMonitorGuardTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IOptions<CrossBorderTransferOptions> _options = Options.Create(new CrossBorderTransferOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new TransferExpirationMonitor(
            null!, _options, _timeProvider,
            NullLogger<TransferExpirationMonitor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new TransferExpirationMonitor(
            _scopeFactory, null!, _timeProvider,
            NullLogger<TransferExpirationMonitor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new TransferExpirationMonitor(
            _scopeFactory, _options, null!,
            NullLogger<TransferExpirationMonitor>.Instance);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new TransferExpirationMonitor(
            _scopeFactory, _options, _timeProvider,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }
}
