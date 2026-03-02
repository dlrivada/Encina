using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionAuditEntryMapper"/> to verify null parameter handling.
/// </summary>
public class RetentionAuditEntryMapperGuardTests
{
    #region ToEntity Guards

    /// <summary>
    /// Verifies that ToEntity throws ArgumentNullException when entry is null.
    /// </summary>
    [Fact]
    public void ToEntity_NullEntry_ThrowsArgumentNullException()
    {
        var act = () => RetentionAuditEntryMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entry");
    }

    #endregion

    #region ToDomain Guards

    /// <summary>
    /// Verifies that ToDomain throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => RetentionAuditEntryMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
