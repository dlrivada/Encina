using Encina.Compliance.BreachNotification.Health;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="BreachNotificationHealthCheck"/> constructor null parameter handling.
/// </summary>
public sealed class BreachNotificationHealthCheckGuardTests
{
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private readonly ILogger<BreachNotificationHealthCheck> _logger =
        NullLogger<BreachNotificationHealthCheck>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var act = () => new BreachNotificationHealthCheck(null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new BreachNotificationHealthCheck(_serviceProvider, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Valid Construction

    [Fact]
    public void Constructor_ValidParameters_DoesNotThrow()
    {
        var act = () => new BreachNotificationHealthCheck(_serviceProvider, _logger);

        Should.NotThrow(act);
    }

    #endregion
}
