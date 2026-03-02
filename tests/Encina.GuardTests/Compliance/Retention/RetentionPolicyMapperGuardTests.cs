using Encina.Compliance.Retention;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionPolicyMapper"/> to verify null parameter handling.
/// </summary>
public class RetentionPolicyMapperGuardTests
{
    #region ToEntity Guards

    /// <summary>
    /// Verifies that ToEntity throws ArgumentNullException when policy is null.
    /// </summary>
    [Fact]
    public void ToEntity_NullPolicy_ThrowsArgumentNullException()
    {
        var act = () => RetentionPolicyMapper.ToEntity(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("policy");
    }

    #endregion

    #region ToDomain Guards

    /// <summary>
    /// Verifies that ToDomain throws ArgumentNullException when entity is null.
    /// </summary>
    [Fact]
    public void ToDomain_NullEntity_ThrowsArgumentNullException()
    {
        var act = () => RetentionPolicyMapper.ToDomain(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("entity");
    }

    #endregion
}
