using Encina.EntityFrameworkCore.Converters;
using Encina.IdGeneration;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Converters;

/// <summary>
/// Unit tests for <see cref="ShardPrefixedIdConverter"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class ShardPrefixedIdConverterTests
{
    private readonly ShardPrefixedIdConverter _sut = new();

    #region Round-Trip Conversion

    [Fact]
    public void RoundTrip_TypicalId_PreservesValue()
    {
        // Arrange
        var original = new ShardPrefixedId("shard-01", "01ARZ3NDEKTSV4RRFFQ69G5FAV");

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_SimpleShardAndSequence_PreservesValue()
    {
        // Arrange
        var original = new ShardPrefixedId("us-east", "12345");

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
        restored.ShardId.ShouldBe("us-east");
        restored.Sequence.ShouldBe("12345");
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("shard-99", "ZZZZZZZZZZZZZZZZZZZZZZZZZZ")]
    [InlineData("region-eu-west-1", "01HGX7PF8V3N9KQWERTY54321")]
    public void RoundTrip_VariousShardAndSequence_PreservesValue(string shardId, string sequence)
    {
        // Arrange
        var original = new ShardPrefixedId(shardId, sequence);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
    }

    #endregion

    #region To Provider (ShardPrefixedId -> string)

    [Fact]
    public void ConvertToProvider_ReturnsColonDelimitedString()
    {
        // Arrange
        var id = new ShardPrefixedId("shard-01", "SEQ123");

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe("shard-01:SEQ123");
    }

    [Fact]
    public void ConvertToProvider_DefaultId_ReturnsEmptyString()
    {
        // Arrange
        var id = default(ShardPrefixedId);

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe(string.Empty);
    }

    #endregion

    #region From Provider (string -> ShardPrefixedId)

    [Fact]
    public void ConvertFromProvider_ValidString_ParsesCorrectly()
    {
        // Arrange
        var dbValue = "shard-01:SEQ123";

        // Act
        var result = ConvertFromProvider(dbValue);

        // Assert
        result.ShardId.ShouldBe("shard-01");
        result.Sequence.ShouldBe("SEQ123");
    }

    [Fact]
    public void ConvertFromProvider_InvalidFormat_ThrowsFormatException()
    {
        // Arrange - no delimiter
        var dbValue = "no-delimiter-here";

        // Act & Assert
        Should.Throw<FormatException>(() => ConvertFromProvider(dbValue));
    }

    [Fact]
    public void ConvertFromProvider_MultipleDelimiters_ThrowsFormatException()
    {
        // Arrange
        var dbValue = "shard:01:extra";

        // Act & Assert
        Should.Throw<FormatException>(() => ConvertFromProvider(dbValue));
    }

    #endregion

    #region Converter Instantiation

    [Fact]
    public void Constructor_CreatesValidConverter()
    {
        // Act
        var converter = new ShardPrefixedIdConverter();

        // Assert
        converter.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private string ConvertToProvider(ShardPrefixedId id)
    {
        // The converter's ConvertToProviderExpression compiles to id => id.ToString()
        var convertToProvider = _sut.ConvertToProviderExpression.Compile();
        return convertToProvider(id);
    }

    private ShardPrefixedId ConvertFromProvider(string value)
    {
        // The converter's ConvertFromProviderExpression compiles to value => ShardPrefixedId.Parse(value)
        var convertFromProvider = _sut.ConvertFromProviderExpression.Compile();
        return convertFromProvider(value);
    }

    #endregion
}
