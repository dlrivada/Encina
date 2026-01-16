using System.Data;
using Microsoft.Extensions.Logging;
using Encina.Dapper.SqlServer;
using Encina.Dapper.SqlServer.Outbox;
using Encina.Messaging.Outbox;

namespace Encina.GuardTests.Dapper.SqlServer;

/// <summary>
/// Guard clause tests for <see cref="OutboxProcessor"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class OutboxProcessorGuardTests
{
    /// <summary>
    /// Tests that constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceProvider serviceProvider = null!;
        var logger = Substitute.For<ILogger<OutboxProcessor>>();
        var options = new OutboxOptions();

        // Act
        var act = () => new OutboxProcessor(serviceProvider, logger, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        ILogger<OutboxProcessor> logger = null!;
        var options = new OutboxOptions();

        // Act
        var act = () => new OutboxProcessor(serviceProvider, logger, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Tests that constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var serviceProvider = Substitute.For<IServiceProvider>();
        var logger = Substitute.For<ILogger<OutboxProcessor>>();
        OutboxOptions options = null!;

        // Act
        var act = () => new OutboxProcessor(serviceProvider, logger, options);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }
}
