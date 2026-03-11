using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public class ServiceCollectionExtensionsGuardTests
{
    #region AddEncinaDPIA Guards

    /// <summary>
    /// Verifies that AddEncinaDPIA throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaDPIA_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddEncinaDPIA();

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    #endregion
}
