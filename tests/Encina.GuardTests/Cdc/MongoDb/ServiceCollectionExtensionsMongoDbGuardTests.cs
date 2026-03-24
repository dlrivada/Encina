using Encina.Cdc.MongoDb;

namespace Encina.GuardTests.Cdc.MongoDb;

/// <summary>
/// Guard clause tests for MongoDB CDC <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsMongoDbGuardTests
{
    #region AddEncinaCdcMongoDb Guards

    /// <summary>
    /// Verifies that AddEncinaCdcMongoDb throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcMongoDb_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcMongoDb(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcMongoDb throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcMongoDb_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcMongoDb(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
