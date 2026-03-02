using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionRecordMapper"/> to verify null parameter handling.
/// </summary>
public class RetentionRecordMapperGuardTests
{
    #region ToEntity Guards

    /// <summary>
    /// Verifies that ToEntity throws ArgumentNullException when record is null.
    /// </summary>
    [Fact]
    public void ToEntity_NullRecord_ThrowsArgumentNullException()
    {
        var act = () => RetentionRecordMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("record");
    }

    #endregion

    #region ToDomain Guards

    /// <summary>
    /// Verifies that ToDomain throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => RetentionRecordMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
