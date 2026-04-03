using Encina.Messaging;
using FsCheck;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Messaging;

/// <summary>
/// Property-based tests for <see cref="SqlIdentifierValidator"/>.
/// </summary>
[Trait("Category", "Property")]
public sealed class SqlIdentifierValidatorPropertyTests
{
    [Property(MaxTest = 100)]
    public bool ValidTableName_AlwaysPassesIsValid(PositiveInt length)
    {
        // Generate valid identifiers: letter + alphanumeric chars
        var len = Math.Min(length.Get, 128);
        var name = "T" + new string('a', len - 1);
        return SqlIdentifierValidator.IsValidTableName(name);
    }

    [Property(MaxTest = 100)]
    public bool ValidateTableName_ValidName_ReturnsSameName(PositiveInt length)
    {
        var len = Math.Min(length.Get, 128);
        var name = "Table" + new string('x', Math.Max(len - 5, 0));
        var result = SqlIdentifierValidator.ValidateTableName(name);
        return result == name;
    }

    [Property(MaxTest = 50)]
    public bool IsValidTableName_NullOrWhitespace_ReturnsFalse(bool useNull)
    {
        var input = useNull ? null : "   ";
        return !SqlIdentifierValidator.IsValidTableName(input);
    }
}
