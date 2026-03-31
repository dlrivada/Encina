using Encina.EntityFrameworkCore.Converters;
using Encina.IdGeneration;
using Shouldly;

namespace Encina.UnitTests.EntityFrameworkCore.Converters;

/// <summary>
/// Unit tests for <see cref="UlidIdConverter"/>.
/// </summary>
[Trait("Category", "Unit")]
public sealed class UlidIdConverterTests
{
    private readonly UlidIdConverter _sut = new();

    #region Round-Trip Conversion

    [Fact]
    public void RoundTrip_NewUlid_PreservesValue()
    {
        // Arrange
        var original = UlidId.NewUlid();

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_KnownTimestamp_PreservesValue()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var original = UlidId.NewUlid(timestamp);

        // Act
        var dbValue = ConvertToProvider(original);
        var restored = ConvertFromProvider(dbValue);

        // Assert
        restored.ShouldBe(original);
    }

    [Fact]
    public void RoundTrip_MultipleIds_AllPreserved()
    {
        // Arrange & Act
        for (var i = 0; i < 10; i++)
        {
            var original = UlidId.NewUlid();
            var dbValue = ConvertToProvider(original);
            var restored = ConvertFromProvider(dbValue);

            // Assert
            restored.ShouldBe(original);
        }
    }

    #endregion

    #region To Provider (UlidId -> string)

    [Fact]
    public void ConvertToProvider_Returns26CharacterCrockfordBase32()
    {
        // Arrange
        var id = UlidId.NewUlid();

        // Act
        var result = ConvertToProvider(id);

        // Assert
        result.Length.ShouldBe(UlidId.StringLength); // 26
    }

    [Fact]
    public void ConvertToProvider_OutputContainsOnlyValidCrockfordBase32Chars()
    {
        // Arrange
        var id = UlidId.NewUlid();
        var validChars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        // Act
        var result = ConvertToProvider(id);

        // Assert
        foreach (var c in result)
        {
            validChars.ShouldContain(c);
        }
    }

    #endregion

    #region From Provider (string -> UlidId)

    [Fact]
    public void ConvertFromProvider_ValidCrockfordBase32_ParsesSuccessfully()
    {
        // Arrange
        var id = UlidId.NewUlid();
        var crockford = id.ToString();

        // Act
        var result = ConvertFromProvider(crockford);

        // Assert
        result.ShouldBe(id);
    }

    [Fact]
    public void ConvertFromProvider_InvalidString_ThrowsFormatException()
    {
        // Arrange
        var invalidString = "NOTAVALIDULIDSTRING!!!!!!!";

        // Act & Assert
        Should.Throw<FormatException>(() => ConvertFromProvider(invalidString));
    }

    [Fact]
    public void ConvertFromProvider_TooShortString_ThrowsFormatException()
    {
        // Arrange
        var shortString = "01ARZ3";

        // Act & Assert
        Should.Throw<FormatException>(() => ConvertFromProvider(shortString));
    }

    #endregion

    #region Converter Instantiation

    [Fact]
    public void Constructor_CreatesValidConverter()
    {
        var converter = new UlidIdConverter();
        converter.ShouldNotBeNull();
    }

    #endregion

    #region Helpers

    private string ConvertToProvider(UlidId id)
    {
        var convertToProvider = _sut.ConvertToProviderExpression.Compile();
        return convertToProvider(id);
    }

    private UlidId ConvertFromProvider(string value)
    {
        var convertFromProvider = _sut.ConvertFromProviderExpression.Compile();
        return convertFromProvider(value);
    }

    #endregion
}
