using Encina.Cdc.PostgreSql;

namespace Encina.GuardTests.Cdc.PostgreSql;

/// <summary>
/// Guard clause tests for PostgreSQL CDC <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsPostgreSqlGuardTests
{
    #region AddEncinaCdcPostgreSql Guards

    /// <summary>
    /// Verifies that AddEncinaCdcPostgreSql throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcPostgreSql_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcPostgreSql(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcPostgreSql throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcPostgreSql_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcPostgreSql(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
