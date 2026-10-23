namespace Encina.GuardTests.Messaging;

/// <summary>
/// Guard tests for <see cref="SqlIdentifierValidator"/>: the null, empty and whitespace checks.
/// </summary>
public sealed class SqlIdentifierValidatorGuardTests
{
    [Fact]
    public void ValidateTableName_NullName_ThrowsArgumentNullExceptionWithParamName()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(null!, "myTable");

        // Assert
        var ex = act.ShouldThrow<ArgumentNullException>();
        ex.ParamName.ShouldBe("myTable");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("\t")]
    public void ValidateTableName_EmptyOrWhitespaceName_ThrowsArgumentExceptionWithParamName(string tableName)
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(tableName, "myTable");

        // Assert
        var ex = act.ShouldThrow<ArgumentException>();
        ex.ParamName.ShouldBe("myTable");
    }

    [Fact]
    public void ValidateTableName_NullName_DefaultParamNameIsTableName()
    {
        // Act
        var act = () => SqlIdentifierValidator.ValidateTableName(null!);

        // Assert
        act.ShouldThrow<ArgumentNullException>().ParamName.ShouldBe("tableName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void IsValidTableName_NullEmptyOrWhitespace_ReturnsFalse(string? tableName)
    {
        // Act
        var result = SqlIdentifierValidator.IsValidTableName(tableName);

        // Assert
        result.ShouldBeFalse();
    }
}
