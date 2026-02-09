using Encina.Cdc.Abstractions;
using Encina.Cdc.Processing;

namespace Encina.GuardTests.Cdc;

/// <summary>
/// Guard clause tests for <see cref="InMemoryCdcPositionStore"/>.
/// Verifies that all null/invalid parameters are properly guarded.
/// </summary>
public sealed class InMemoryCdcPositionStoreGuardTests
{
    private readonly InMemoryCdcPositionStore _store = new();

    #region GetPositionAsync Guards

    /// <summary>
    /// Verifies that GetPositionAsync throws ArgumentNullException when connectorId is null.
    /// </summary>
    [Fact]
    public async Task GetPositionAsync_NullConnectorId_ShouldThrowArgumentNullException()
    {
        // Arrange
        string connectorId = null!;

        // Act
        var act = () => _store.GetPositionAsync(connectorId);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that GetPositionAsync throws ArgumentException when connectorId is empty.
    /// </summary>
    [Fact]
    public async Task GetPositionAsync_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _store.GetPositionAsync("");

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that GetPositionAsync throws ArgumentException when connectorId is whitespace.
    /// </summary>
    [Fact]
    public async Task GetPositionAsync_WhitespaceConnectorId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _store.GetPositionAsync("   ");

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    #endregion

    #region SavePositionAsync Guards

    /// <summary>
    /// Verifies that SavePositionAsync throws ArgumentNullException when connectorId is null.
    /// </summary>
    [Fact]
    public async Task SavePositionAsync_NullConnectorId_ShouldThrowArgumentNullException()
    {
        // Arrange
        string connectorId = null!;
        var position = new TestCdcPosition(1);

        // Act
        var act = () => _store.SavePositionAsync(connectorId, position);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that SavePositionAsync throws ArgumentException when connectorId is empty.
    /// </summary>
    [Fact]
    public async Task SavePositionAsync_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var position = new TestCdcPosition(1);

        // Act
        var act = () => _store.SavePositionAsync("", position);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that SavePositionAsync throws ArgumentException when connectorId is whitespace.
    /// </summary>
    [Fact]
    public async Task SavePositionAsync_WhitespaceConnectorId_ShouldThrowArgumentException()
    {
        // Arrange
        var position = new TestCdcPosition(1);

        // Act
        var act = () => _store.SavePositionAsync("   ", position);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that SavePositionAsync throws ArgumentNullException when position is null.
    /// </summary>
    [Fact]
    public async Task SavePositionAsync_NullPosition_ShouldThrowArgumentNullException()
    {
        // Arrange
        CdcPosition position = null!;

        // Act
        var act = () => _store.SavePositionAsync("connector-1", position);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("position");
    }

    #endregion

    #region DeletePositionAsync Guards

    /// <summary>
    /// Verifies that DeletePositionAsync throws ArgumentNullException when connectorId is null.
    /// </summary>
    [Fact]
    public async Task DeletePositionAsync_NullConnectorId_ShouldThrowArgumentNullException()
    {
        // Arrange
        string connectorId = null!;

        // Act
        var act = () => _store.DeletePositionAsync(connectorId);

        // Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that DeletePositionAsync throws ArgumentException when connectorId is empty.
    /// </summary>
    [Fact]
    public async Task DeletePositionAsync_EmptyConnectorId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _store.DeletePositionAsync("");

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    /// <summary>
    /// Verifies that DeletePositionAsync throws ArgumentException when connectorId is whitespace.
    /// </summary>
    [Fact]
    public async Task DeletePositionAsync_WhitespaceConnectorId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _store.DeletePositionAsync("   ");

        // Assert
        var ex = await Should.ThrowAsync<ArgumentException>(act);
        ex.ParamName.ShouldBe("connectorId");
    }

    #endregion

    #region Test Helpers

    private sealed class TestCdcPosition : CdcPosition
    {
        public TestCdcPosition(long value) => Value = value;
        public long Value { get; }
        public override byte[] ToBytes() => BitConverter.GetBytes(Value);
        public override int CompareTo(CdcPosition? other) =>
            other is TestCdcPosition tcp ? Value.CompareTo(tcp.Value) : 1;
        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    #endregion
}
