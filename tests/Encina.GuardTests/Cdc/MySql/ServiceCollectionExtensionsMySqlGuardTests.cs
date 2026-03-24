using Encina.Cdc.MySql;

namespace Encina.GuardTests.Cdc.MySql;

/// <summary>
/// Guard clause tests for MySQL CDC <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsMySqlGuardTests
{
    #region AddEncinaCdcMySql Guards

    /// <summary>
    /// Verifies that AddEncinaCdcMySql throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcMySql_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcMySql(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcMySql throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcMySql_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcMySql(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
