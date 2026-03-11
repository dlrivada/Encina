using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DPIAAssessmentMapper"/> to verify null parameter handling.
/// </summary>
public class DPIAAssessmentMapperGuardTests
{
    #region ToEntity Guards

    /// <summary>
    /// Verifies that ToEntity throws ArgumentNullException when assessment is null.
    /// </summary>
    [Fact]
    public void ToEntity_NullAssessment_ThrowsArgumentNullException()
    {
        var act = () => DPIAAssessmentMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("assessment");
    }

    #endregion

    #region ToDomain Guards

    /// <summary>
    /// Verifies that ToDomain throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => DPIAAssessmentMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
