using Encina.Cdc;

namespace Encina.GuardTests.Cdc;

/// <summary>
/// Guard clause tests for <see cref="CdcConfiguration"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class CdcConfigurationGuardTests
{
    #region WithTableMapping Guards

    /// <summary>
    /// Verifies that WithTableMapping throws ArgumentNullException when tableName is null.
    /// </summary>
    [Fact]
    public void WithTableMapping_NullTableName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new CdcConfiguration();
        string tableName = null!;

        // Act
        var act = () => config.WithTableMapping<object>(tableName);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that WithTableMapping throws ArgumentException when tableName is empty.
    /// </summary>
    [Fact]
    public void WithTableMapping_EmptyTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new CdcConfiguration();

        // Act
        var act = () => config.WithTableMapping<object>("");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    /// <summary>
    /// Verifies that WithTableMapping throws ArgumentException when tableName is whitespace.
    /// </summary>
    [Fact]
    public void WithTableMapping_WhitespaceTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new CdcConfiguration();

        // Act
        var act = () => config.WithTableMapping<object>("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("tableName");
    }

    #endregion

    #region WithOptions Guards

    /// <summary>
    /// Verifies that WithOptions throws ArgumentNullException when configure action is null.
    /// </summary>
    [Fact]
    public void WithOptions_NullConfigure_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new CdcConfiguration();
        Action<CdcOptions> configure = null!;

        // Act
        var act = () => config.WithOptions(configure);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    #endregion

    #region UseOutboxCdc Guards

    /// <summary>
    /// Verifies that UseOutboxCdc throws ArgumentNullException when outboxTableName is null.
    /// </summary>
    [Fact]
    public void UseOutboxCdc_NullOutboxTableName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var config = new CdcConfiguration();
        string outboxTableName = null!;

        // Act
        var act = () => config.UseOutboxCdc(outboxTableName);

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("outboxTableName");
    }

    /// <summary>
    /// Verifies that UseOutboxCdc throws ArgumentException when outboxTableName is empty.
    /// </summary>
    [Fact]
    public void UseOutboxCdc_EmptyOutboxTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new CdcConfiguration();

        // Act
        var act = () => config.UseOutboxCdc("");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("outboxTableName");
    }

    /// <summary>
    /// Verifies that UseOutboxCdc throws ArgumentException when outboxTableName is whitespace.
    /// </summary>
    [Fact]
    public void UseOutboxCdc_WhitespaceOutboxTableName_ShouldThrowArgumentException()
    {
        // Arrange
        var config = new CdcConfiguration();

        // Act
        var act = () => config.UseOutboxCdc("   ");

        // Assert
        var ex = Should.Throw<ArgumentException>(act);
        ex.ParamName.ShouldBe("outboxTableName");
    }

    #endregion
}
