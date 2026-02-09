using Encina.Cdc;
using Encina.Cdc.Messaging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Cdc.Messaging;

/// <summary>
/// Guard clause tests for <see cref="CdcMessagingBridge"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class CdcMessagingBridgeGuardTests
{
    private readonly IEncina _encina;
    private readonly CdcMessagingOptions _options;
    private readonly ILogger<CdcMessagingBridge> _logger;

    public CdcMessagingBridgeGuardTests()
    {
        _encina = Substitute.For<IEncina>();
        _options = new CdcMessagingOptions();
        _logger = NullLogger<CdcMessagingBridge>.Instance;
    }

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when encina is null.
    /// </summary>
    [Fact]
    public void Constructor_NullEncina_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcMessagingBridge(null!, _options, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("encina");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when options is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcMessagingBridge(_encina, null!, _logger);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new CdcMessagingBridge(_encina, _options, null!);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region OnEventDispatchedAsync Guards

    /// <summary>
    /// Verifies that OnEventDispatchedAsync throws ArgumentNullException when changeEvent is null.
    /// </summary>
    [Fact]
    public async Task OnEventDispatchedAsync_NullChangeEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var bridge = new CdcMessagingBridge(_encina, _options, _logger);
        ChangeEvent changeEvent = null!;

        // Act
        var act = async () => await bridge.OnEventDispatchedAsync(changeEvent);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("changeEvent");
    }

    #endregion
}
