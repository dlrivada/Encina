using Encina.Compliance.Anonymization;

namespace Encina.GuardTests.Compliance.Anonymization;

/// <summary>
/// Guard tests for <see cref="TokenMappingMapper"/> to verify null parameter handling.
/// </summary>
public class TokenMappingMapperGuardTests
{
    #region ToEntity Guards

    [Fact]
    public void ToEntity_NullMapping_ThrowsArgumentNullException()
    {
        var act = () => TokenMappingMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("mapping");
    }

    #endregion

    #region ToDomain Guards

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => TokenMappingMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
