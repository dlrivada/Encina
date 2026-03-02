using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="LegalHoldMapper"/> to verify null parameter handling.
/// </summary>
public class LegalHoldMapperGuardTests
{
    #region ToEntity Guards

    /// <summary>
    /// Verifies that ToEntity throws ArgumentNullException when hold is null.
    /// </summary>
    [Fact]
    public void ToEntity_NullHold_ThrowsArgumentNullException()
    {
        var act = () => LegalHoldMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("hold");
    }

    #endregion

    #region ToDomain Guards

    /// <summary>
    /// Verifies that ToDomain throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => LegalHoldMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
