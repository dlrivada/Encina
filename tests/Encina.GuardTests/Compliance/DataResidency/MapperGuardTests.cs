using Encina.Compliance.DataResidency;

using Shouldly;

namespace Encina.GuardTests.Compliance.DataResidency;

public class MapperGuardTests
{
    #region DataLocationMapper Guards

    [Fact]
    public void DataLocationMapper_ToEntity_NullLocation_ShouldThrow()
    {
        var act = () => DataLocationMapper.ToEntity(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void DataLocationMapper_ToDomain_NullEntity_ShouldThrow()
    {
        var act = () => DataLocationMapper.ToDomain(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region ResidencyPolicyMapper Guards

    [Fact]
    public void ResidencyPolicyMapper_ToEntity_NullPolicy_ShouldThrow()
    {
        var act = () => ResidencyPolicyMapper.ToEntity(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ResidencyPolicyMapper_ToDomain_NullEntity_ShouldThrow()
    {
        var act = () => ResidencyPolicyMapper.ToDomain(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion

    #region ResidencyAuditEntryMapper Guards

    [Fact]
    public void ResidencyAuditEntryMapper_ToEntity_NullEntry_ShouldThrow()
    {
        var act = () => ResidencyAuditEntryMapper.ToEntity(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    public void ResidencyAuditEntryMapper_ToDomain_NullEntity_ShouldThrow()
    {
        var act = () => ResidencyAuditEntryMapper.ToDomain(null!);
        Should.Throw<ArgumentNullException>(act);
    }

    #endregion
}
