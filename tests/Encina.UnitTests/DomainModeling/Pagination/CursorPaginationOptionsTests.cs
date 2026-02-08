using Encina.DomainModeling.Pagination;
using FluentAssertions;

namespace Encina.UnitTests.DomainModeling.Pagination;

/// <summary>
/// Unit tests for <see cref="CursorPaginationOptions"/> and <see cref="CursorDirection"/>.
/// </summary>
public class CursorPaginationOptionsTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_ShouldHaveCorrectValues()
    {
        // Act
        var options = new CursorPaginationOptions();

        // Assert
        options.Cursor.Should().BeNull();
        options.PageSize.Should().Be(20);
        options.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void Constructor_WithAllParameters_ShouldSetCorrectly()
    {
        // Act
        var options = new CursorPaginationOptions(
            Cursor: "test-cursor",
            PageSize: 50,
            Direction: CursorDirection.Backward);

        // Assert
        options.Cursor.Should().Be("test-cursor");
        options.PageSize.Should().Be(50);
        options.Direction.Should().Be(CursorDirection.Backward);
    }

    #endregion

    #region Default Static Property Tests

    [Fact]
    public void Default_ShouldHaveCorrectValues()
    {
        // Act
        var options = CursorPaginationOptions.Default;

        // Assert
        options.Cursor.Should().BeNull();
        options.PageSize.Should().Be(CursorPaginationOptions.DefaultPageSize);
        options.Direction.Should().Be(CursorDirection.Forward);
    }

    [Fact]
    public void Default_ShouldBeImmutable()
    {
        // Act
        var options1 = CursorPaginationOptions.Default;
        var options2 = CursorPaginationOptions.Default;

        // Assert - Same reference
        ReferenceEquals(options1, options2).Should().BeTrue();
    }

    #endregion

    #region Constants Tests

    [Fact]
    public void MaxPageSize_ShouldBe100()
    {
        // Assert
        CursorPaginationOptions.MaxPageSize.Should().Be(100);
    }

    [Fact]
    public void DefaultPageSize_ShouldBe20()
    {
        // Assert
        CursorPaginationOptions.DefaultPageSize.Should().Be(20);
    }

    #endregion

    #region WithCursor Tests

    [Fact]
    public void WithCursor_ShouldReturnNewInstanceWithCursor()
    {
        // Arrange
        var original = CursorPaginationOptions.Default;

        // Act
        var modified = original.WithCursor("new-cursor");

        // Assert
        modified.Cursor.Should().Be("new-cursor");
        modified.PageSize.Should().Be(original.PageSize);
        modified.Direction.Should().Be(original.Direction);
        original.Cursor.Should().BeNull(); // Original unchanged
    }

    [Fact]
    public void WithCursor_NullCursor_ShouldWork()
    {
        // Arrange
        var original = new CursorPaginationOptions(Cursor: "existing");

        // Act
        var modified = original.WithCursor(null);

        // Assert
        modified.Cursor.Should().BeNull();
    }

    #endregion

    #region WithSize Tests

    [Fact]
    public void WithSize_ValidSize_ShouldReturnNewInstance()
    {
        // Arrange
        var original = CursorPaginationOptions.Default;

        // Act
        var modified = original.WithSize(50);

        // Assert
        modified.PageSize.Should().Be(50);
        modified.Cursor.Should().Be(original.Cursor);
        modified.Direction.Should().Be(original.Direction);
    }

    [Fact]
    public void WithSize_MinimumValue_ShouldSucceed()
    {
        // Act
        var options = CursorPaginationOptions.Default.WithSize(1);

        // Assert
        options.PageSize.Should().Be(1);
    }

    [Fact]
    public void WithSize_MaximumValue_ShouldSucceed()
    {
        // Act
        var options = CursorPaginationOptions.Default.WithSize(CursorPaginationOptions.MaxPageSize);

        // Assert
        options.PageSize.Should().Be(100);
    }

    [Fact]
    public void WithSize_ZeroSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSize(0);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("pageSize");
    }

    [Fact]
    public void WithSize_NegativeSize_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSize(-1);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("pageSize");
    }

    [Fact]
    public void WithSize_ExceedsMaximum_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = CursorPaginationOptions.Default;

        // Act & Assert
        var action = () => options.WithSize(101);
        action.Should().Throw<ArgumentOutOfRangeException>()
            .And.ParamName.Should().Be("pageSize");
    }

    #endregion

    #region WithDirection Tests

    [Fact]
    public void WithDirection_Backward_ShouldReturnNewInstance()
    {
        // Arrange
        var original = CursorPaginationOptions.Default;

        // Act
        var modified = original.WithDirection(CursorDirection.Backward);

        // Assert
        modified.Direction.Should().Be(CursorDirection.Backward);
        modified.Cursor.Should().Be(original.Cursor);
        modified.PageSize.Should().Be(original.PageSize);
    }

    [Fact]
    public void WithDirection_Forward_ShouldWork()
    {
        // Arrange
        var original = new CursorPaginationOptions(Direction: CursorDirection.Backward);

        // Act
        var modified = original.WithDirection(CursorDirection.Forward);

        // Assert
        modified.Direction.Should().Be(CursorDirection.Forward);
    }

    #endregion

    #region IsFirstPage Tests

    [Fact]
    public void IsFirstPage_NullCursor_ShouldBeTrue()
    {
        // Arrange
        var options = new CursorPaginationOptions(Cursor: null);

        // Assert
        options.IsFirstPage.Should().BeTrue();
    }

    [Fact]
    public void IsFirstPage_EmptyCursor_ShouldBeTrue()
    {
        // Arrange
        var options = new CursorPaginationOptions(Cursor: "");

        // Assert
        options.IsFirstPage.Should().BeTrue();
    }

    [Fact]
    public void IsFirstPage_WithCursor_ShouldBeFalse()
    {
        // Arrange
        var options = new CursorPaginationOptions(Cursor: "some-cursor");

        // Assert
        options.IsFirstPage.Should().BeFalse();
    }

    #endregion

    #region Fluent Builder Chain Tests

    [Fact]
    public void FluentChain_ShouldBuildCorrectOptions()
    {
        // Act
        var options = CursorPaginationOptions.Default
            .WithCursor("test-cursor")
            .WithSize(50)
            .WithDirection(CursorDirection.Backward);

        // Assert
        options.Cursor.Should().Be("test-cursor");
        options.PageSize.Should().Be(50);
        options.Direction.Should().Be(CursorDirection.Backward);
    }

    [Fact]
    public void FluentChain_ShouldNotModifyOriginal()
    {
        // Arrange
        var original = CursorPaginationOptions.Default;

        // Act
        _ = original
            .WithCursor("cursor")
            .WithSize(50)
            .WithDirection(CursorDirection.Backward);

        // Assert - Original should be unchanged
        original.Cursor.Should().BeNull();
        original.PageSize.Should().Be(20);
        original.Direction.Should().Be(CursorDirection.Forward);
    }

    #endregion

    #region CursorDirection Enum Tests

    [Fact]
    public void CursorDirection_Forward_ShouldBeZero()
    {
        // Assert
        ((int)CursorDirection.Forward).Should().Be(0);
    }

    [Fact]
    public void CursorDirection_Backward_ShouldBeOne()
    {
        // Assert
        ((int)CursorDirection.Backward).Should().Be(1);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        var options1 = new CursorPaginationOptions("cursor", 50, CursorDirection.Forward);
        var options2 = new CursorPaginationOptions("cursor", 50, CursorDirection.Forward);

        // Assert
        options1.Should().Be(options2);
        (options1 == options2).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCursor_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new CursorPaginationOptions("cursor1", 50, CursorDirection.Forward);
        var options2 = new CursorPaginationOptions("cursor2", 50, CursorDirection.Forward);

        // Assert
        options1.Should().NotBe(options2);
    }

    [Fact]
    public void Equality_DifferentPageSize_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new CursorPaginationOptions("cursor", 50, CursorDirection.Forward);
        var options2 = new CursorPaginationOptions("cursor", 100, CursorDirection.Forward);

        // Assert
        options1.Should().NotBe(options2);
    }

    [Fact]
    public void Equality_DifferentDirection_ShouldNotBeEqual()
    {
        // Arrange
        var options1 = new CursorPaginationOptions("cursor", 50, CursorDirection.Forward);
        var options2 = new CursorPaginationOptions("cursor", 50, CursorDirection.Backward);

        // Assert
        options1.Should().NotBe(options2);
    }

    #endregion
}
