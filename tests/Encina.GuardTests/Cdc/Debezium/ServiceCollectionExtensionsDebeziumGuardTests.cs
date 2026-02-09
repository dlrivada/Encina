using Encina.Cdc.Debezium;
using Encina.Cdc.Debezium.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Cdc.Debezium;

/// <summary>
/// Guard clause tests for Debezium <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded for both HTTP and Kafka registration methods.
/// </summary>
public sealed class ServiceCollectionExtensionsDebeziumGuardTests
{
    #region AddEncinaCdcDebezium Guards

    /// <summary>
    /// Verifies that AddEncinaCdcDebezium throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebezium_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcDebezium(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcDebezium throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebezium_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcDebezium(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion

    #region AddEncinaCdcDebeziumKafka Guards

    /// <summary>
    /// Verifies that AddEncinaCdcDebeziumKafka throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebeziumKafka_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaCdcDebeziumKafka(_ => { });

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    /// <summary>
    /// Verifies that AddEncinaCdcDebeziumKafka throws ArgumentNullException when configure is null.
    /// </summary>
    [Fact]
    public void AddEncinaCdcDebeziumKafka_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddEncinaCdcDebeziumKafka(null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion
}
