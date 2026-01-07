using Shouldly;

namespace Encina.Messaging.Tests;

/// <summary>
/// Unit tests for <see cref="SqlIdentifierValidator"/>.
/// </summary>
public sealed class SqlIdentifierValidatorTests
{
    #region ValidateTableName Tests

    [Theory]
    [InlineData("Users")]
    [InlineData("Orders")]
    [InlineData("_private")]
    [InlineData("Table1")]
    [InlineData("User_Orders")]
    [InlineData("dbo.Users")]
    [InlineData("schema.TableName")]
    [InlineData("[Users]")]
    [InlineData("[dbo].[Users]")]
    [InlineData("_underscore_start")]
    public void ValidateTableName_ValidNames_ReturnsName(string tableName)
    {
        // Act
        var result = SqlIdentifierValidator.ValidateTableName(tableName);

        // Assert
        result.ShouldBe(tableName);
    }

    [Fact]
    public void ValidateTableName_NullName_ThrowsArgumentException()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(null!);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ValidateTableName_EmptyName_ThrowsArgumentException()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(string.Empty);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ValidateTableName_WhitespaceName_ThrowsArgumentException()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName("   ");

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Fact]
    public void ValidateTableName_TooLongName_ThrowsArgumentException()
    {
        // Arrange
        var longName = new string('a', 129);

        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(longName);

        // Assert
        var ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldContain("maximum length of 128");
    }

    [Fact]
    public void ValidateTableName_ExactlyMaxLength_Succeeds()
    {
        // Arrange
        var exactName = new string('a', 128);

        // Act
        var result = SqlIdentifierValidator.ValidateTableName(exactName);

        // Assert
        result.ShouldBe(exactName);
    }

    [Theory]
    [InlineData("1Table")] // Starts with digit
    [InlineData("Table Name")] // Contains space
    [InlineData("Table-Name")] // Contains hyphen
    [InlineData("Table;DROP")] // Contains semicolon
    [InlineData("'injection'")] // Contains quotes
    [InlineData("Table\nName")] // Contains newline
    [InlineData("schema..table")] // Double dot
    [InlineData("Table@Name")] // Contains @ symbol
    public void ValidateTableName_InvalidNames_ThrowsArgumentException(string tableName)
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(tableName);

        // Assert
        var ex = act.ShouldThrow<ArgumentException>();
        ex.Message.ShouldContain("invalid characters");
    }

    [Fact]
    public void ValidateTableName_CustomParamName_UsesInException()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(null!, "customParam");

        // Assert
        var ex = act.ShouldThrow<ArgumentException>();
        ex.ParamName.ShouldBe("customParam");
    }

    #endregion

    #region IsValidTableName Tests

    [Theory]
    [InlineData("Users", true)]
    [InlineData("Orders", true)]
    [InlineData("_private", true)]
    [InlineData("Table1", true)]
    [InlineData("User_Orders", true)]
    [InlineData("dbo.Users", true)]
    [InlineData("[Users]", true)]
    [InlineData("[dbo].[Users]", true)]
    public void IsValidTableName_ValidNames_ReturnsTrue(string tableName, bool expected)
    {
        // Act
        var result = SqlIdentifierValidator.IsValidTableName(tableName);

        // Assert
        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidTableName_NullOrWhitespace_ReturnsFalse(string? tableName)
    {
        // Act
        var result = SqlIdentifierValidator.IsValidTableName(tableName);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsValidTableName_TooLong_ReturnsFalse()
    {
        // Arrange
        var longName = new string('a', 129);

        // Act
        var result = SqlIdentifierValidator.IsValidTableName(longName);

        // Assert
        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData("1Table")]
    [InlineData("Table Name")]
    [InlineData("Table-Name")]
    [InlineData("Table;DROP")]
    [InlineData("'injection'")]
    public void IsValidTableName_InvalidNames_ReturnsFalse(string tableName)
    {
        // Act
        var result = SqlIdentifierValidator.IsValidTableName(tableName);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
