using Encina.Compliance.BreachNotification;
using Encina.Compliance.BreachNotification.Detection;

namespace Encina.GuardTests.Compliance.BreachNotification;

/// <summary>
/// Guard tests for <see cref="DefaultBreachDetector"/> method-level null parameter handling.
/// Constructor guards are covered in <see cref="DefaultBreachDetectorGuardTests"/>.
/// </summary>
public class DefaultBreachDetectorMethodGuardTests
{
    private readonly DefaultBreachDetector _sut;

    public DefaultBreachDetectorMethodGuardTests()
    {
        _sut = new DefaultBreachDetector([], NullLogger<DefaultBreachDetector>.Instance);
    }

    #region DetectAsync

    [Fact]
    public async Task DetectAsync_NullSecurityEvent_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.DetectAsync(null!);

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("securityEvent");
    }

    #endregion

    #region RegisterDetectionRule

    [Fact]
    public void RegisterDetectionRule_NullRule_ThrowsArgumentNullException()
    {
        var act = () => _sut.RegisterDetectionRule(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("rule");
    }

    #endregion
}
