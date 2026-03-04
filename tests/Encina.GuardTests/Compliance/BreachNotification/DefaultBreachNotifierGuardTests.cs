using Encina.Compliance.BreachNotification;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachNotifier"/> to verify null parameter handling.
/// </summary>
public class DefaultBreachNotifierGuardTests
{
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultBreachNotifier> _logger = NullLogger<DefaultBreachNotifier>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when timeProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotifier(null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachNotifier(_timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
