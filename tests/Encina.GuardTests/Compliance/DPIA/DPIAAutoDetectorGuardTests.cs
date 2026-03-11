using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAAutoDetector"/> to verify null parameter handling.
/// </summary>
public class DPIAAutoDetectorGuardTests
{
    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DPIAAutoDetector(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region DetectHighRiskTriggers Guards

    /// <summary>
    /// Verifies that DetectHighRiskTriggers throws ArgumentNullException when type is null.
    /// </summary>
    [Fact]
    public void DetectHighRiskTriggers_NullType_ThrowsArgumentNullException()
    {
        var sut = new DPIAAutoDetector(NullLogger.Instance);

        var act = () => sut.DetectHighRiskTriggers(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("type");
    }

    #endregion
}
