using Encina.Cdc.SqlServer;

namespace Encina.GuardTests.Cdc.SqlServer;

/// <summary>
/// Guard clause tests for SQL Server CDC <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsSqlServerGuardTests
{
    #region AddEncinaCdcSqlServer Guards

    /// <summary>
    /// Verifies that AddEncinaCdcSqlServer throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcSqlServer_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcSqlServer(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcSqlServer throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcSqlServer_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcSqlServer(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
