using Encina.Compliance.BreachNotification;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachDeadlineMonitorService"/> constructor null parameter handling.
/// </summary>
public sealed class BreachDeadlineMonitorServiceGuardTests
{
    private readonly IServiceScopeFactory _scopeFactory = Substitute.For<IServiceScopeFactory>();
    private readonly IOptions<BreachNotificationOptions> _options = Options.Create(new BreachNotificationOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;

    private readonly ILogger<BreachDeadlineMonitorService> _logger =
        NullLogger<BreachDeadlineMonitorService>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        var act = () => new BreachDeadlineMonitorService(
            null!, _options, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new BreachDeadlineMonitorService(
            _scopeFactory, null!, _timeProvider, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new BreachDeadlineMonitorService(
            _scopeFactory, _options, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new BreachDeadlineMonitorService(
            _scopeFactory, _options, _timeProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion
}
