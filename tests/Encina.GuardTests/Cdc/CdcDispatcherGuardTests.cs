using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Cdc;

/// <summary>
/// Guard clause tests for <see cref="CdcDispatcher"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class CdcDispatcherGuardTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CdcDispatcher> _logger;
    private readonly CdcConfiguration _configuration;

    public CdcDispatcherGuardTests()
    {
        _serviceProvider = Substitute.For<IServiceProvider>();
        _logger = NullLogger<CdcDispatcher>.Instance;
        _configuration = new CdcConfiguration();
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when serviceProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullServiceProvider_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcDispatcher(null!, _logger, _configuration);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcDispatcher(_serviceProvider, null!, _configuration);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when configuration is null.
    /// </summary>
    [Fact]
    public void Constructor_NullConfiguration_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcDispatcher(_serviceProvider, _logger, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configuration");
    }

    #endregion

    #region DispatchAsync Guards

    /// <summary>
    /// Verifies that DispatchAsync throws ArgumentNullException when changeEvent is null.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_NullChangeEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = new CdcDispatcher(_serviceProvider, _logger, _configuration);
        ChangeEvent changeEvent = null!;

        // Act
        var act = async () => await dispatcher.DispatchAsync(changeEvent);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("changeEvent");
    }

    #endregion
}
