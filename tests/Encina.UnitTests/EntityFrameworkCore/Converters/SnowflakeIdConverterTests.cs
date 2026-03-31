using Encina.EntityFrameworkCore.Converters;
using Encina.IdGeneration;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Converters;

/// <summary>
/// Unit tests for <see cref="SnowflakeIdConverter"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SnowflakeIdConverterTests
{
    private readonly SnowflakeIdConverter _sut = new();

    #region Round-Trip Conversion

    [Theory]
    [InlineData(0L)]
    [InlineData(1L)]
    [InlineData(123456789L)]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    public void RoundTrip_VariousValues_PreservesValue(long rawValue)
    {
        // Arrange
        var original = new SnowflakeId(rawValue);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
        restored.Value.ShouldBe(rawValue);
    }

    [Fact]
    public void RoundTrip_DefaultId_PreservesValue()
    {
        // Arrange
        var original = default(SnowflakeId);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
        restored.Value.ShouldBe(0L);
    }

    #endregion

    #region To Provider (SnowflakeId -> long)

    [Fact]
    public void ConvertToProvider_ReturnsRawLongValue()
    {
        // Arrange
        var id = new SnowflakeId(42L);

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe(42L);
    }

    [Fact]
    public void ConvertToProvider_EmptyId_ReturnsZero()
    {
        // Arrange
        var id = SnowflakeId.Empty;

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.ShouldBe(0L);
    }

    #endregion

    #region From Provider (long -> SnowflakeId)

    [Fact]
    public void ConvertFromProvider_PositiveValue_CreatesCorrectId()
    {
        // Arrange
        var dbValue = 999999999L;

        // Act
        var result = ConvertFromProvider(dbValue);

        // Assert
        result.Value.ShouldBe(999999999L);
        result.IsEmpty.ShouldBeFalse();
    }

    [Fact]
    public void ConvertFromProvider_Zero_CreatesEmptyId()
    {
        // Act
        var result = ConvertFromProvider(0L);

        // Assert
        result.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ConvertFromProvider_NegativeValue_CreatesIdWithNegativeValue()
    {
        // Act
        var result = ConvertFromProvider(-1L);

        // Assert
        result.Value.ShouldBe(-1L);
    }

    #endregion

    #region Converter Instantiation

    [Fact]
    public void Constructor_CreatesValidConverter()
    {
        var converter = new SnowflakeIdConverter();
        converter.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private long ConvertToProvider(SnowflakeId id)
    {
        var convertToProvider = _sut.ConvertToProviderExpression.Compile();
        return convertToProvider(id);
    }

    private SnowflakeId ConvertFromProvider(long value)
    {
        var convertFromProvider = _sut.ConvertFromProviderExpression.Compile();
        return convertFromProvider(value);
    }

    #endregion
}
