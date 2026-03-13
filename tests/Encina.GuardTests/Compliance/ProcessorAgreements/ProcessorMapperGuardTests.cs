using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorMapper"/> to verify null parameter handling.
/// </summary>
public class ProcessorMapperGuardTests
{
    #region ToEntity Guards

    [Fact]
    public void ToEntity_NullProcessor_ThrowsArgumentNullException()
    {
        var act = () => ProcessorMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("processor");
    }

    #endregion

    #region ToDomain Guards

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => ProcessorMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
