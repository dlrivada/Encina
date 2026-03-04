using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachDetector"/> to verify null parameter handling.
/// </summary>
public class DefaultBreachDetectorGuardTests
{
    private readonly IEnumerable<IBreachDetectionRule> _rules = [];
    private readonly ILogger<DefaultBreachDetector> _logger = NullLogger<DefaultBreachDetector>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when rules is null.
    /// </summary>
    [Fact]
    public void Constructor_NullRules_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachDetector(null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rules");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultBreachDetector(_rules, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion
}
