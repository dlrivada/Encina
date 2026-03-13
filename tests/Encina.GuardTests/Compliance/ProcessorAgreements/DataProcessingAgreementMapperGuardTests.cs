using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="DataProcessingAgreementMapper"/> to verify null parameter handling.
/// </summary>
public class DataProcessingAgreementMapperGuardTests
{
    #region ToEntity Guards

    [Fact]
    public void ToEntity_NullAgreement_ThrowsArgumentNullException()
    {
        var act = () => DataProcessingAgreementMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("agreement");
    }

    #endregion

    #region ToDomain Guards

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => DataProcessingAgreementMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
