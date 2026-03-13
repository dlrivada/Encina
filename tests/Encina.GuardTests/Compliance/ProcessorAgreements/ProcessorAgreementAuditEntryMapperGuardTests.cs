using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorAgreementAuditEntryMapper"/> to verify null parameter handling.
/// </summary>
public class ProcessorAgreementAuditEntryMapperGuardTests
{
    #region ToEntity Guards

    [Fact]
    public void ToEntity_NullEntry_ThrowsArgumentNullException()
    {
        var act = () => ProcessorAgreementAuditEntryMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region ToDomain Guards

    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => ProcessorAgreementAuditEntryMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
